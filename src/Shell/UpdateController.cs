using Serilog;
using Velopack;

namespace Micser.Shell;

internal enum UpdateCheckResult
{
    UpToDate,
    UpdateReady,
    Failed,
}

/// <summary>
/// Checks for updates in the background (30 s after the start, then every 12 h) and on request, and installs a downloaded update. Shared by
/// the tray and the window; runs on the UI thread, where its events are raised.
/// </summary>
internal sealed class UpdateController : IDisposable
{
    public const string ReleaseNotesFileName = "ReleaseNotes.md";

    private static readonly TimeSpan CheckDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(12);

    private readonly CancellationTokenSource _cancellation = new();
    private readonly EngineSupervisor _supervisor;
    private readonly Updater _updater;
    private Task<UpdateCheckResult>? _check;

    public UpdateController(Updater updater, EngineSupervisor supervisor)
    {
        _updater = updater;
        _supervisor = supervisor;
        PendingUpdate = updater.PendingUpdate;
    }

    /// <summary>
    /// Raised when <see cref="IsChecking"/> or <see cref="PendingUpdate"/> changes.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Raised before the shell exits to install an update, so the window and the tray icon can close.
    /// </summary>
    public event EventHandler? Installing;

    public SemanticVersion CurrentVersion => _updater.CurrentVersion;

    public bool IsChecking => _check != null;

    /// <summary>
    /// The downloaded update that is installed on the next start or by <see cref="InstallAsync"/>, or null.
    /// </summary>
    public VelopackAsset? PendingUpdate { get; private set; }

    /// <summary>
    /// Downloads a newer release, if there is one. A check that is already running is joined.
    /// </summary>
    public Task<UpdateCheckResult> CheckAsync()
    {
        return _check ??= RunCheckAsync();
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    /// <summary>
    /// Returns the markdown release notes of the installed version (from <see cref="ReleaseNotesFileName"/>, written by scripts/pack.ps1)
    /// or of the downloaded update (from its package), or null for another version or a release without notes.
    /// </summary>
    public string? GetReleaseNotes(string version)
    {
        if (version == CurrentVersion.ToString())
        {
            var path = Path.Combine(AppContext.BaseDirectory, ReleaseNotesFileName);
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Warning(ex, "Reading the release notes failed.");
                return null;
            }
        }

        return PendingUpdate is { } update && version == update.Version.ToString() && !string.IsNullOrWhiteSpace(update.NotesMarkdown)
            ? update.NotesMarkdown
            : null;
    }

    /// <summary>
    /// Stops the engine and restarts the shell into the pending update.
    /// </summary>
    /// <param name="minimized">Whether the restarted shell stays in the tray.</param>
    public async Task InstallAsync(bool minimized)
    {
        if (PendingUpdate is not { } update)
        {
            return;
        }

        Log.Information("Restarting to install version {Version}.", update.Version);
        Installing?.Invoke(this, EventArgs.Empty);
        await _supervisor.ShutdownEngineAsync();
        _updater.ApplyAndRestart(update, minimized);
    }

    /// <summary>
    /// Starts the background checks. Call on the UI thread.
    /// </summary>
    public void Start()
    {
        _ = CheckPeriodicallyAsync(_cancellation.Token);
    }

    private async Task CheckPeriodicallyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(CheckDelay, cancellationToken);
            using var timer = new PeriodicTimer(CheckInterval);
            do
            {
                await CheckAsync();
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task<UpdateCheckResult> RunCheckAsync()
    {
        // continue after CheckAsync stored the task, so IsChecking is true for the handlers
        await Task.Yield();
        Changed?.Invoke(this, EventArgs.Empty);
        try
        {
            PendingUpdate = await _updater.DownloadAsync(_cancellation.Token);
            return PendingUpdate != null ? UpdateCheckResult.UpdateReady : UpdateCheckResult.UpToDate;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Warning(ex, "Checking for updates failed.");
            return UpdateCheckResult.Failed;
        }
        finally
        {
            _check = null;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
