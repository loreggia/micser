using Serilog;
using Velopack;

namespace Micser.Shell;

/// <summary>
/// Keeps the shell alive in the notification area. The main window is created on demand.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly TimeSpan UpdateCheckDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(12);

    private readonly RegisteredWaitHandle _activationRegistration;
    private readonly Autostart _autostart;
    private readonly ToolStripMenuItem _autostartItem;
    private readonly NotifyIcon _notifyIcon;
    private readonly ShellOptions _options;
    private readonly EngineSupervisor _supervisor;
    private readonly SynchronizationContext _uiContext;
    private readonly CancellationTokenSource _updateCancellation = new();
    private readonly ToolStripMenuItem? _updateItem;
    private readonly Updater? _updater;
    private bool _isCheckingForUpdates;
    private MainForm? _mainForm;
    private VelopackAsset? _pendingUpdate;

    /// <param name="updater">Null if this copy can't update (development).</param>
    /// <param name="activation">Signaled by another shell instance to show the window.</param>
    public TrayApplicationContext(ShellOptions options, EngineSupervisor supervisor, Updater? updater, EventWaitHandle activation)
    {
        _options = options;
        _supervisor = supervisor;
        _updater = updater;
        _uiContext = SynchronizationContext.Current ?? throw new InvalidOperationException("Create the tray on the UI thread.");
        _autostart = new Autostart(Application.ExecutablePath);

        _autostartItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleAutostart()) { Checked = _autostart.IsEnabled };
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Open", null, (_, _) => ShowMainForm()) { Font = new Font(menu.Font, FontStyle.Bold) });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_autostartItem);
        if (_updater != null)
        {
            _pendingUpdate = _updater.PendingUpdate;
            _updateItem = new ToolStripMenuItem("", null, async (_, _) => await OnUpdateItemClickedAsync())
            {
                ToolTipText = $"Installed version: {_updater.CurrentVersion}",
            };
            UpdateUpdateItem();
            menu.Items.Add(_updateItem);
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Close", null, (_, _) => ExitThread()) { ToolTipText = "Closes the window and the tray icon. Audio keeps running." });
        menu.Items.Add(new ToolStripMenuItem("Exit Micser", null, async (_, _) => await ExitMicserAsync()) { ToolTipText = "Stops the audio engine and closes Micser." });
        menu.ShowItemToolTips = true;

        _notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ShowMainForm();

        _supervisor.Changed += (_, _) => UpdateStatus();
        UpdateStatus();
        _supervisor.Start();

        _activationRegistration = ThreadPool.RegisterWaitForSingleObject(
            activation, (_, _) => _uiContext.Post(_ => ShowMainForm(), null), null, Timeout.Infinite, executeOnlyOnce: false);

        if (!options.StartMinimized)
        {
            ShowMainForm();
        }

        if (_updater != null)
        {
            _ = CheckForUpdatesPeriodicallyAsync(_updateCancellation.Token);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activationRegistration.Unregister(null);
            _updateCancellation.Cancel();
            _updateCancellation.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _mainForm?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task ApplyUpdateAsync(VelopackAsset update)
    {
        Log.Information("Restarting to install version {Version}.", update.Version);
        var minimized = _mainForm == null || _mainForm.IsDisposed || !_mainForm.Visible;
        _notifyIcon.Text = "Micser – updating";
        _mainForm?.Close();
        await _supervisor.ShutdownEngineAsync();
        _notifyIcon.Visible = false;
        _updater!.ApplyAndRestart(update, minimized);
    }

    /// <summary>
    /// Downloads a newer release, if there is one. With <paramref name="notify"/>, the result is always shown; otherwise only a new update is.
    /// </summary>
    private async Task CheckForUpdatesAsync(bool notify)
    {
        if (_isCheckingForUpdates)
        {
            return;
        }

        _isCheckingForUpdates = true;
        UpdateUpdateItem();
        var previous = _pendingUpdate;
        try
        {
            _pendingUpdate = await _updater!.DownloadAsync(_updateCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Checking for updates failed.");
            if (notify)
            {
                _notifyIcon.ShowBalloonTip(5000, "Micser", "Checking for updates failed.", ToolTipIcon.Warning);
            }
        }
        finally
        {
            _isCheckingForUpdates = false;
            UpdateUpdateItem();
        }

        if (_pendingUpdate != null && (notify || _pendingUpdate.Version != previous?.Version))
        {
            _notifyIcon.ShowBalloonTip(
                5000, $"Micser {_pendingUpdate.Version} is ready", "Choose \"Restart to update\" in the tray menu to install it.", ToolTipIcon.Info);
        }
        else if (_pendingUpdate == null && notify)
        {
            _notifyIcon.ShowBalloonTip(5000, "Micser", $"Micser {_updater!.CurrentVersion} is up to date.", ToolTipIcon.Info);
        }
    }

    private async Task CheckForUpdatesPeriodicallyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(UpdateCheckDelay, cancellationToken);
            using var timer = new PeriodicTimer(UpdateCheckInterval);
            do
            {
                await CheckForUpdatesAsync(notify: false);
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ExitMicserAsync()
    {
        _notifyIcon.Text = "Micser – stopping";
        _mainForm?.Close();
        await _supervisor.ShutdownEngineAsync();
        ExitThread();
    }

    private async Task OnUpdateItemClickedAsync()
    {
        if (_pendingUpdate != null)
        {
            await ApplyUpdateAsync(_pendingUpdate);
        }
        else
        {
            await CheckForUpdatesAsync(notify: true);
        }
    }

    private void ShowMainForm()
    {
        if (_mainForm == null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(_supervisor, _options.UiUrl, WindowSettings.DefaultPath);
        }

        _mainForm.Show();
        if (_mainForm.WindowState == FormWindowState.Minimized)
        {
            _mainForm.WindowState = FormWindowState.Normal;
        }

        _mainForm.Activate();
    }

    private void ToggleAutostart()
    {
        try
        {
            _autostart.SetEnabled(!_autostart.IsEnabled);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            Log.Error(ex, "Changing the autostart entry failed.");
        }

        _autostartItem.Checked = _autostart.IsEnabled;
    }

    private void UpdateUpdateItem()
    {
        _updateItem!.Enabled = !_isCheckingForUpdates;
        _updateItem.Text = _isCheckingForUpdates ? "Checking for updates…"
            : _pendingUpdate != null ? $"Restart to update to {_pendingUpdate.Version}"
            : "Check for updates";
    }

    private void UpdateStatus()
    {
        _notifyIcon.Text = _supervisor.State switch
        {
            EngineState.Running => "Micser",
            EngineState.Starting => "Micser – starting the audio engine",
            _ => "Micser – the audio engine isn't running",
        };
    }
}
