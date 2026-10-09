using Velopack;

namespace Micser.Shell;

/// <summary>
/// Velopack's install, update and uninstall hooks (registered in <c>Program.Main</c>). Velopack runs the shell with special arguments for them and
/// exits it afterwards. Before updating and uninstalling, Velopack ends all processes in the app folder: for updates after the hook (so the engine
/// is stopped gracefully there), for uninstalls before it.
/// </summary>
internal static class InstallHooks
{
    // fast callbacks are killed after 15 s
    private static readonly TimeSpan EngineStopTimeout = TimeSpan.FromSeconds(10);

    public static void AfterInstall(SemanticVersion _)
    {
        SetAutostart(true);
    }

    public static void BeforeUninstall(SemanticVersion _)
    {
        SetAutostart(false);
    }

    /// <summary>
    /// Stops the engine gracefully, so it saves its configuration before Velopack ends all processes in the app folder.
    /// </summary>
    public static void BeforeUpdate(SemanticVersion _)
    {
        try
        {
            using var http = new HttpClient();
            using var timeout = new CancellationTokenSource(EngineStopTimeout);
            var engine = new EngineLocator(EngineLocator.DefaultDiscoveryPath, http)
                .FindAsync(timeout.Token)
                .GetAwaiter()
                .GetResult();
            if (engine != null)
            {
                EngineControl.ShutdownAsync(http, engine, timeout.Token).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or ArgumentException or OperationCanceledException)
        {
            // Velopack ends the process
        }
    }

    private static void SetAutostart(bool enabled)
    {
        try
        {
            var autostart = new Autostart(Environment.ProcessPath!);
            if (enabled || autostart.IsEnabled)
            {
                autostart.SetEnabled(enabled);
            }
        }
        catch (Exception ex)
            when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            // the user can change it in the tray menu
        }
    }
}
