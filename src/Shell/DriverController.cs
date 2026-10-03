using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// What <c>Micser.DriverUtility status</c> reports (see its DriverStatus).
/// </summary>
internal sealed record DriverStatus(bool Installed, uint? Problem, string? InstalledVersion, string? BundledVersion, int CableCount, bool UpdateAvailable)
{
    public IReadOnlyList<CableStatus> Cables { get; init; } = [];
}

/// <param name="Layout">"stereo", "5.1" or "7.1".</param>
/// <param name="FormatsMatch">Whether the cable's endpoints have its layout's format.</param>
internal sealed record CableStatus(string Layout, bool FormatsMatch);

internal enum DriverCommandResult
{
    Success,
    RebootRequired,

    /// <summary>
    /// The user declined the UAC prompt.
    /// </summary>
    Cancelled,

    Failed,
}

/// <summary>
/// Installs and configures the virtual audio cable driver through the DriverUtility that Velopack installs in the app's driver folder.
/// Every change runs the utility elevated (one UAC prompt) while the engine is stopped. Runs on the UI thread, where its events are
/// raised.
/// </summary>
internal sealed class DriverController
{
    private const int ErrorCancelled = 1223;
    private const int ExitRebootRequired = 3010;
    private const int ExitSuccess = 0;

    private static readonly JsonSerializerOptions StatusJson = new(JsonSerializerDefaults.Web);

    private readonly EngineSupervisor _supervisor;
    private readonly string _utilityPath;

    private DriverController(string utilityPath, EngineSupervisor supervisor)
    {
        _utilityPath = utilityPath;
        _supervisor = supervisor;
    }

    /// <summary>
    /// Raised when <see cref="Status"/> or <see cref="IsBusy"/> changes.
    /// </summary>
    public event EventHandler? Changed;

    public bool IsBusy { get; private set; }

    /// <summary>
    /// The last status, or null until <see cref="RefreshAsync"/> got one.
    /// </summary>
    public DriverStatus? Status { get; private set; }

    /// <summary>
    /// The controller for the utility in the app's driver folder, or null if this copy has none (development, or a release without a
    /// signed driver).
    /// </summary>
    public static DriverController? Create(EngineSupervisor supervisor)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "driver", "Micser.DriverUtility.exe");
        return File.Exists(path) ? new DriverController(path, supervisor) : null;
    }

    public Task<DriverCommandResult> InstallAsync(int cableCount)
    {
        return RunAsync("install", "--count", cableCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Reads the driver status (no elevation needed). When a cable's endpoints don't have its layout's format (e.g. a layout change that
    /// needed a reboot), sets them with <c>sync-formats</c>, which needs no elevation either.
    /// </summary>
    public async Task RefreshAsync()
    {
        Status = await ReadStatusAsync();
        if (Status is { Installed: true, Problem: null } && Status.Cables.Any(c => !c.FormatsMatch))
        {
            Log.Information("Setting the formats of the virtual audio cables' endpoints.");
            var (exitCode, error) = await RunUtilityAsync("sync-formats");
            if (exitCode != ExitSuccess)
            {
                Log.Warning("Setting the cables' formats failed ({ExitCode}): {Error}", exitCode, error);
            }

            Status = await ReadStatusAsync();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Task<DriverCommandResult> SetCableCountAsync(int cableCount)
    {
        return RunAsync("set-count", cableCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <param name="layout">"stereo", "5.1" or "7.1".</param>
    public Task<DriverCommandResult> SetCableLayoutAsync(int cable, string layout)
    {
        return RunAsync("set-layout", cable.ToString(System.Globalization.CultureInfo.InvariantCulture), layout);
    }

    public Task<DriverCommandResult> UninstallAsync()
    {
        return RunAsync("uninstall");
    }

    public Task<DriverCommandResult> UpdateAsync()
    {
        return RunAsync("update");
    }

    private async Task<DriverCommandResult> RunAsync(params string[] arguments)
    {
        if (IsBusy)
        {
            return DriverCommandResult.Failed;
        }

        IsBusy = true;
        Changed?.Invoke(this, EventArgs.Empty);
        try
        {
            var result = await _supervisor.RunWithoutEngineAsync(() => RunElevatedAsync(arguments));
            Log.Information("Driver {Command}: {Result}", arguments[0], result);
            return result;
        }
        finally
        {
            IsBusy = false;
            await RefreshAsync();
        }
    }

    private async Task<DriverStatus?> ReadStatusAsync()
    {
        var (exitCode, output) = await RunUtilityAsync("status", readOutput: true);
        try
        {
            return exitCode == ExitSuccess ? JsonSerializer.Deserialize<DriverStatus>(output, StatusJson) : null;
        }
        catch (JsonException ex)
        {
            Log.Warning(ex, "Reading the driver status failed.");
            return null;
        }
    }

    /// <summary>
    /// Runs the utility without elevation. Returns its exit code (-1 if it didn't start) and its standard output, or its standard error
    /// unless <paramref name="readOutput"/>.
    /// </summary>
    private async Task<(int ExitCode, string Text)> RunUtilityAsync(string command, bool readOutput = false)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(_utilityPath, command)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, readOutput ? await output : await error);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Log.Warning(ex, "Running the driver utility ({Command}) failed.", command);
            return (-1, "");
        }
    }

    private async Task<DriverCommandResult> RunElevatedAsync(string[] arguments)
    {
        var startInfo = new ProcessStartInfo(_utilityPath)
        {
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(startInfo)!;
            await process.WaitForExitAsync();
            return process.ExitCode switch
            {
                ExitSuccess => DriverCommandResult.Success,
                ExitRebootRequired => DriverCommandResult.RebootRequired,
                _ => DriverCommandResult.Failed,
            };
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            return DriverCommandResult.Cancelled;
        }
        catch (Win32Exception ex)
        {
            Log.Error(ex, "Starting the driver utility failed.");
            return DriverCommandResult.Failed;
        }
    }
}
