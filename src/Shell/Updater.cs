using Velopack;
using Velopack.Sources;

namespace Micser.Shell;

/// <summary>
/// Checks for and downloads updates from the GitHub releases, and applies them. Only an installed copy can update.
/// </summary>
internal sealed class Updater
{
    private const string RepositoryUrl = "https://github.com/loreggia/micser";

    // a local folder or URL with a Velopack release feed, for testing updates
    private const string SourceVariable = "MICSER_UPDATE_SOURCE";

    private readonly UpdateManager _manager;

    private Updater(UpdateManager manager)
    {
        _manager = manager;
    }

    public SemanticVersion CurrentVersion => _manager.CurrentVersion!;

    /// <summary>
    /// The downloaded update that is applied on the next start, or null.
    /// </summary>
    public VelopackAsset? PendingUpdate => _manager.UpdatePendingRestart;

    /// <summary>
    /// Restarts the shell into the pending update. The engine must be stopped first.
    /// </summary>
    public void ApplyAndRestart(VelopackAsset update, bool minimized)
    {
        _manager.ApplyUpdatesAndRestart(update, minimized ? [ShellOptions.MinimizedArgument] : []);
    }

    /// <summary>
    /// Returns null if this copy isn't installed (development).
    /// </summary>
    public static Updater? Create()
    {
        var source = Environment.GetEnvironmentVariable(SourceVariable);
        var manager = string.IsNullOrEmpty(source)
            ? new UpdateManager(new GithubSource(RepositoryUrl, null, false))
            : new UpdateManager(source);
        return manager.IsInstalled ? new Updater(manager) : null;
    }

    /// <summary>
    /// Downloads the latest release if it's newer. Returns the update that is ready to apply, or null if there is none.
    /// </summary>
    public async Task<VelopackAsset?> DownloadAsync(CancellationToken cancellationToken)
    {
        var update = await _manager.CheckForUpdatesAsync();
        if (update == null)
        {
            return PendingUpdate;
        }

        await _manager.DownloadUpdatesAsync(update, cancelToken: cancellationToken);
        return update.TargetFullRelease;
    }
}
