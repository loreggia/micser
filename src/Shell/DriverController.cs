using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// What <c>Micser.DriverUtility status</c> reports (see its DriverStatus).
/// </summary>
internal sealed record DriverStatus(bool Installed, uint? Problem, string? InstalledVersion, string? BundledVersion, int CableCount, bool UpdateAvailable);

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
    /// Reads the driver status (no elevation needed).
    /// </summary>
    public async Task RefreshAsync()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(_utilityPath, "status")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })!;
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            Status = JsonSerializer.Deserialize<DriverStatus>(output, StatusJson);
        }
        catch (Exception ex) when (ex is Win32Exception or JsonException or InvalidOperationException)
        {
            Log.Warning(ex, "Reading the driver status failed.");
            Status = null;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Task<DriverCommandResult> SetCableCountAsync(int cableCount)
    {
        return RunAsync("set-count", cableCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
