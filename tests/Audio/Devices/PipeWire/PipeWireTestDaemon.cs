using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Micser.Audio.Devices;
using Micser.Audio.Devices.PipeWire;
using TUnit.Core.Interfaces;

namespace Micser.Audio.Tests.Devices.PipeWire;

/// <summary>
/// A private PipeWire daemon with WirePlumber (on a private D-Bus session) for the test session, with virtual devices only: it has its
/// own runtime, configuration and state folders and doesn't load any device monitor, so the session's audio and devices stay untouched.
/// Needs <c>pipewire</c>, <c>wireplumber</c> 0.4 (Lua configuration) and <c>dbus-run-session</c>; <see cref="Unavailable"/> says why
/// it didn't start otherwise.
/// </summary>
public sealed class PipeWireTestDaemon : IAsyncInitializer, IAsyncDisposable
{
    public const string SinkName = "micser-test-sink";
    public const string SourceName = "micser-test-source";
    public const string SurroundName = "micser-test-surround";

    private const string PipeWireConfig = """
        context.properties = {
            core.daemon = true
            core.name = pipewire-0
            link.max-buffers = 16
            default.clock.rate = 48000
        }
        context.spa-libs = {
            audio.convert.* = audioconvert/libspa-audioconvert
            support.*       = support/libspa-support
        }
        context.modules = [
            { name = libpipewire-module-protocol-native }
            { name = libpipewire-module-metadata }
            { name = libpipewire-module-spa-node-factory }
            { name = libpipewire-module-client-node }
            { name = libpipewire-module-adapter }
            { name = libpipewire-module-link-factory }
            { name = libpipewire-module-session-manager }
            { name = libpipewire-module-access }
        ]
        context.objects = [
            { factory = spa-node-factory
                args = { factory.name = support.node.driver node.name = Dummy-Driver node.group = pipewire.dummy priority.driver = 20000 } }
            { factory = adapter
                args = { factory.name = support.null-audio-sink node.name = micser-test-sink node.description = "Test Sink" media.class = Audio/Sink audio.position = "FL,FR" priority.session = 1000 } }
            { factory = adapter
                args = { factory.name = support.null-audio-sink node.name = micser-test-surround node.description = "Test Surround" media.class = Audio/Sink audio.position = "FL,FR,RL,RR,FC,LFE" } }
            { factory = adapter
                args = { factory.name = support.null-audio-sink node.name = micser-test-source node.description = "Test Source" media.class = Audio/Source/Virtual audio.position = "MONO" } }
        ]
        """;

    // WirePlumber 0.4's main.lua.d/90-enable-all.lua without the device monitors and the idle suspension
    private const string WirePlumberMain = """
        load_module("metadata")
        default_access.enable()
        device_defaults.enable()
        stream_defaults.enable()
        load_script("intended-roles.lua")
        load_script("sm-objects.lua")
        """;

    private readonly List<Process> _processes = [];
    private string? _directory;
    private string? _pipeWireConfig;
    private string? _wirePlumberConfig;

    /// <summary>
    /// The daemon's socket, for <see cref="PipeWireBackend.TryCreate"/>.
    /// </summary>
    public string Socket => Path.Combine(_directory!, "pipewire-0");

    /// <summary>
    /// Why the daemon isn't running, or null if it is.
    /// </summary>
    public string? Unavailable { get; private set; }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        if (_directory != null)
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    public async Task InitializeAsync()
    {
        if (!OperatingSystem.IsLinux())
        {
            Unavailable = "PipeWire runs on Linux only";
            return;
        }

        Unavailable = FindMissingRequirement();
        if (Unavailable != null)
        {
            return;
        }

        // the socket path is limited to 108 bytes, so the folder can't be in a long temp path
        var root = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") is { Length: > 0 and < 60 } runtime
            ? runtime
            : "/tmp";
        _directory = Path.Combine(root, $"micser-pw-test-{Environment.ProcessId}");
        Directory.CreateDirectory(
            _directory,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
        );
        _pipeWireConfig = Directory.CreateDirectory(Path.Combine(_directory, "pipewire")).FullName;
        await File.WriteAllTextAsync(Path.Combine(_pipeWireConfig, "pipewire.conf"), PipeWireConfig);
        foreach (var client in new[] { "client.conf", "client-rt.conf" })
        {
            File.Copy(Path.Combine("/usr/share/pipewire", client), Path.Combine(_pipeWireConfig, client));
        }

        _wirePlumberConfig = Path.Combine(_directory, "wireplumber");
        CopyDirectory("/usr/share/wireplumber", _wirePlumberConfig);
        await File.WriteAllTextAsync(
            Path.Combine(_wirePlumberConfig, "main.lua.d", "90-enable-all.lua"),
            WirePlumberMain
        );
        await File.WriteAllTextAsync(Path.Combine(_wirePlumberConfig, "bluetooth.lua.d", "90-enable-all.lua"), "");

        if (!await StartAsync())
        {
            Unavailable = "the test PipeWire daemon didn't start";
        }
    }

    /// <summary>
    /// Stops the daemons and starts them again, as an update of PipeWire would.
    /// </summary>
    [SupportedOSPlatform("linux")]
    public async Task RestartAsync()
    {
        await StopAsync();
        if (!await StartAsync())
        {
            throw new InvalidOperationException("The test PipeWire daemon didn't start again.");
        }
    }

    /// <summary>
    /// The global id of the node named <paramref name="nodeName"/>, from <c>pw-dump</c>.
    /// </summary>
    public async Task<uint?> FindNodeIdAsync(string nodeName)
    {
        using var dump = JsonDocument.Parse(await RunAsync("pw-dump", "-r", Socket));
        foreach (var item in dump.RootElement.EnumerateArray())
        {
            if (
                item.GetProperty("type").GetString() == "PipeWire:Interface:Node"
                && item.GetProperty("info").GetProperty("props").TryGetProperty("node.name", out var name)
                && name.GetString() == nodeName
            )
            {
                return item.GetProperty("id").GetUInt32();
            }
        }

        return null;
    }

    /// <summary>
    /// Runs <c>pw-metadata</c> against the daemon's <c>default</c> metadata, e.g. to set a property.
    /// </summary>
    public Task<string> PwMetadataAsync(params string[] arguments)
    {
        return RunAsync("pw-metadata", ["-r", Socket, .. arguments]);
    }

    /// <summary>
    /// Runs <c>pw-cli</c> against the daemon, e.g. to create or destroy a node.
    /// </summary>
    public Task<string> PwCliAsync(params string[] arguments)
    {
        return RunAsync("pw-cli", ["-r", Socket, .. arguments]);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    [SupportedOSPlatform("linux")]
    private static string? FindMissingRequirement()
    {
        if (!NativeLibrary.TryLoad(PipeWireNative.Library, out _))
        {
            return $"{PipeWireNative.Library} isn't installed";
        }

        foreach (
            var program in new[] { "pipewire", "wireplumber", "dbus-run-session", "pw-cli", "pw-dump", "pw-metadata" }
        )
        {
            var paths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(':');
            if (!paths.Any(path => File.Exists(Path.Combine(path, program))))
            {
                return $"{program} isn't installed";
            }
        }

        return Directory.Exists("/usr/share/wireplumber/main.lua.d")
            ? null
            : "the test daemon needs WirePlumber 0.4's Lua configuration";
    }

    private static async Task<string> RunAsync(string program, params string[] arguments)
    {
        var info = new ProcessStartInfo(program, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(info)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return output;
    }

    private void Start(string program, params string[] arguments)
    {
        var info = new ProcessStartInfo(program, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Environment =
            {
                ["PIPEWIRE_RUNTIME_DIR"] = _directory,
                ["XDG_RUNTIME_DIR"] = _directory,
                ["XDG_CONFIG_HOME"] = Path.Combine(_directory!, "config"),
                ["XDG_STATE_HOME"] = Path.Combine(_directory!, "state"),
                ["PIPEWIRE_CONFIG_DIR"] = _pipeWireConfig,
                ["WIREPLUMBER_CONFIG_DIR"] = _wirePlumberConfig,
            },
        };
        var process = Process.Start(info)!;

        // the logs aren't needed, but full pipes would block the daemons
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _processes.Add(process);
    }

    /// <summary>
    /// Starts PipeWire and WirePlumber and waits until they're ready.
    /// </summary>
    [SupportedOSPlatform("linux")]
    private async Task<bool> StartAsync()
    {
        File.Delete(Socket);
        Start("pipewire");
        for (var i = 0; i < 50 && !File.Exists(Socket); i++)
        {
            await Task.Delay(100);
        }

        Start("dbus-run-session", "--", "wireplumber");
        return await WaitForDefaultSinkAsync();
    }

    private async Task StopAsync()
    {
        foreach (var process in Enumerable.Reverse(_processes))
        {
            try
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            catch (InvalidOperationException)
            {
                // already exited
            }

            process.Dispose();
        }

        _processes.Clear();
    }

    /// <summary>
    /// WirePlumber is ready once it chose a default sink.
    /// </summary>
    [SupportedOSPlatform("linux")]
    private async Task<bool> WaitForDefaultSinkAsync()
    {
        for (var i = 0; i < 50; i++)
        {
            using var backend = PipeWireBackend.TryCreate(NullLogger.Instance, Socket);
            if (backend?.GetDefaultDevice(DeviceDirection.Output) != null)
            {
                return true;
            }

            await Task.Delay(200);
        }

        return false;
    }
}
