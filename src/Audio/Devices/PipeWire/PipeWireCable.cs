using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using static Micser.Audio.Devices.PipeWire.PipeWireNative;

namespace Micser.Audio.Devices.PipeWire;

/// <summary>
/// A virtual audio cable: PipeWire's loopback module in the engine's context, with a virtual sink ("Micser Cable N Input", which apps
/// play to) joined to a virtual source ("Micser Cable N Output", which apps record from), both in the cable's layout. The cable lives
/// as long as the module: the backend unloads it before the connection closes and loads it again after reconnecting. A module also
/// destroys itself when its connection breaks; the destroy listener notices, so it's never destroyed twice.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed unsafe class PipeWireCable : IDisposable
{
    private static readonly ImplModuleEvents* EventTable = CreateEvents();

    private readonly GCHandle _handle;
    private readonly SpaHook* _hook;
    private readonly ILogger _logger;
    private nint _module;

    public PipeWireCable(int number, ChannelLayout layout, ILogger logger)
    {
        Number = number;
        Layout = layout;
        _logger = logger;
        _handle = GCHandle.Alloc(this);
        _hook = (SpaHook*)NativeMemory.AllocZeroed((nuint)sizeof(SpaHook));
    }

    public bool IsLoaded => _module != 0;

    public ChannelLayout Layout { get; }

    public int Number { get; }

    /// <summary>
    /// The node name (device id) of the cable's sink, which apps play to.
    /// </summary>
    public static string InputName(int number)
    {
        return $"micser_cable_{number}_input";
    }

    /// <summary>
    /// The node name (device id) of the cable's source, which apps record from.
    /// </summary>
    public static string OutputName(int number)
    {
        return $"micser_cable_{number}_output";
    }

    /// <summary>
    /// Unloads the cable and frees it. Loop lock held.
    /// </summary>
    public void Dispose()
    {
        Unload();
        NativeMemory.Free(_hook);
        _handle.Free();
    }

    /// <summary>
    /// Creates the cable's nodes. Loop lock held.
    /// </summary>
    /// <param name="remote">The daemon the backend connected to, for a module that makes its own connection.</param>
    public void Load(nint context, string? remote)
    {
        if (_module != 0)
        {
            return;
        }

        _module = ContextLoadModule(context, "libpipewire-module-loopback", GetArguments(remote), 0);
        if (_module == 0)
        {
            _logger.LogWarning(
                "Could not create virtual cable {Number}: {Error}.",
                Number,
                Marshal.GetLastPInvokeErrorMessage()
            );
            return;
        }

        ImplModuleAddListener(_module, _hook, (nint)EventTable, GCHandle.ToIntPtr(_handle));
        _logger.LogInformation("Created virtual cable {Number} ({Layout}).", Number, Layout);
    }

    /// <summary>
    /// Removes the cable's nodes. Loop lock held.
    /// </summary>
    public void Unload()
    {
        if (_module == 0)
        {
            return;
        }

        var module = _module;
        _module = 0;
        HookRemove(_hook);
        ImplModuleDestroy(module);
    }

    private static ImplModuleEvents* CreateEvents()
    {
        var events = (ImplModuleEvents*)NativeMemory.AllocZeroed((nuint)sizeof(ImplModuleEvents));
        events->Destroy = &OnDestroy;
        return events;
    }

    /// <summary>
    /// A string in SPA-JSON.
    /// </summary>
    private static string Quote(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    [UnmanagedCallersOnly]
    private static void OnDestroy(nint data)
    {
        // the module destroyed itself (e.g. its connection broke); its listeners go with it
        if (GCHandle.FromIntPtr(data).Target is PipeWireCable cable)
        {
            HookRemove(cable._hook);
            cable._module = 0;
        }
    }

    private string GetArguments(string? remote)
    {
        var positions = string.Join(" ", SpaAudioChannels.ToNames(Layout));
        var name = $"Micser Cable {Number}";
        return $$"""
            {
                node.description = {{Quote(name)}}
                audio.position = [ {{positions}} ]
                {{(remote == null ? "" : "remote.name = " + Quote(remote))}}
                capture.props = {
                    node.name = {{Quote(InputName(Number))}}
                    node.description = {{Quote(name + " Input")}}
                    media.class = "Audio/Sink"
                }
                playback.props = {
                    node.name = {{Quote(OutputName(Number))}}
                    node.description = {{Quote(name + " Output")}}
                    media.class = "Audio/Source"
                }
            }
            """;
    }
}
