using Serilog;

namespace Micser.Shell;

/// <summary>
/// Keeps the shell alive in the notification area. The main window is created on demand.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly RegisteredWaitHandle _activationRegistration;
    private readonly Autostart _autostart;
    private readonly ToolStripMenuItem _autostartItem;
    private readonly NotifyIcon _notifyIcon;
    private readonly ShellOptions _options;
    private readonly DriverController? _driver;
    private readonly EngineSupervisor _supervisor;
    private readonly SynchronizationContext _uiContext;
    private readonly UpdateController? _updates;
    private readonly ToolStripMenuItem? _updateItem;
    private MainForm? _mainForm;
    private string? _notifiedVersion;

    /// <param name="updates">Null if this copy can't update (development).</param>
    /// <param name="driver">Null if this copy has no driver package.</param>
    /// <param name="activation">Signaled by another shell instance to show the window.</param>
    public TrayApplicationContext(ShellOptions options, EngineSupervisor supervisor, UpdateController? updates, DriverController? driver, EventWaitHandle activation)
    {
        _options = options;
        _supervisor = supervisor;
        _updates = updates;
        _driver = driver;
        _uiContext = SynchronizationContext.Current ?? throw new InvalidOperationException("Create the tray on the UI thread.");
        _autostart = new Autostart(Application.ExecutablePath);

        _autostartItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleAutostart()) { Checked = _autostart.IsEnabled };
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Open", null, (_, _) => ShowMainForm()) { Font = new Font(menu.Font, FontStyle.Bold) });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_autostartItem);
        if (_updates != null)
        {
            _notifiedVersion = _updates.PendingUpdate?.Version.ToString();
            _updateItem = new ToolStripMenuItem("", null, async (_, _) => await OnUpdateItemClickedAsync())
            {
                ToolTipText = $"Installed version: {_updates.CurrentVersion}",
            };
            menu.Items.Add(_updateItem);
            _updates.Changed += (_, _) => OnUpdatesChanged();
            _updates.Installing += (_, _) =>
            {
                _notifyIcon!.Text = "Micser – updating";
                _mainForm?.Close();
                _notifyIcon.Visible = false;
            };
            OnUpdatesChanged();
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

        _updates?.Start();
        _ = CheckDriverAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activationRegistration.Unregister(null);
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _mainForm?.Dispose();
        }

        base.Dispose(disposing);
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
        if (_updates!.PendingUpdate != null)
        {
            await _updates.InstallAsync(minimized: _mainForm == null || _mainForm.IsDisposed || !_mainForm.Visible);
            return;
        }

        switch (await _updates.CheckAsync())
        {
            case UpdateCheckResult.UpToDate:
                _notifyIcon.ShowBalloonTip(5000, "Micser", $"Micser {_updates.CurrentVersion} is up to date.", ToolTipIcon.Info);
                break;
            case UpdateCheckResult.Failed:
                _notifyIcon.ShowBalloonTip(5000, "Micser", "Checking for updates failed.", ToolTipIcon.Warning);
                break;
        }
    }

    private void OnUpdatesChanged()
    {
        var pending = _updates!.PendingUpdate;
        _updateItem!.Enabled = !_updates.IsChecking;
        _updateItem.Text = _updates.IsChecking ? "Checking for updates…"
            : pending != null ? $"Restart to update to {pending.Version}"
            : "Check for updates";

        // each new update is announced once, whether the check ran in the background or was requested
        var version = pending?.Version.ToString();
        if (version != null && version != _notifiedVersion)
        {
            _notifyIcon?.ShowBalloonTip(5000, $"Micser {version} is ready", "Choose \"Restart to update\" in the tray menu to install it.", ToolTipIcon.Info);
        }

        _notifiedVersion = version;
    }

    private void ShowMainForm()
    {
        if (_mainForm == null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(_supervisor, _updates, _driver, _options.UiUrl, WindowSettings.DefaultPath);
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

    /// <summary>
    /// Reads the driver status and points out a newer bundled driver once per start.
    /// </summary>
    private async Task CheckDriverAsync()
    {
        if (_driver == null)
        {
            return;
        }

        await _driver.RefreshAsync();
        if (_driver.Status is { UpdateAvailable: true } status)
        {
            _notifyIcon.ShowBalloonTip(
                10000,
                "Micser",
                $"A new virtual audio cable driver ({status.BundledVersion}) is available. Install it in Micser's settings.",
                ToolTipIcon.Info);
        }
    }

    private void UpdateStatus()
    {
        _notifyIcon.Text = _supervisor.State switch
        {
            EngineState.Running => "Micser",
            EngineState.Starting => "Micser – starting the audio engine",
            EngineState.Paused => "Micser – changing the virtual audio cables",
            _ => "Micser – the audio engine isn't running",
        };
    }
}
