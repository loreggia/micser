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
    private readonly ToolStripMenuItem _closeItem;
    private readonly DriverController? _driver;
    private readonly ToolStripMenuItem _exitItem;
    private readonly ShellLanguage _language;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _openItem;
    private readonly ShellOptions _options;
    private readonly EngineSupervisor _supervisor;
    private readonly SynchronizationContext _uiContext;
    private readonly UpdateController? _updates;
    private readonly ToolStripMenuItem? _updateItem;
    private MainForm? _mainForm;
    private string? _notifiedVersion;

    /// <param name="updates">Null if this copy can't update (development).</param>
    /// <param name="driver">Null if this copy has no driver package.</param>
    /// <param name="activation">Signaled by another shell instance to show the window.</param>
    public TrayApplicationContext(
        ShellOptions options,
        EngineSupervisor supervisor,
        UpdateController? updates,
        DriverController? driver,
        ShellLanguage language,
        EventWaitHandle activation
    )
    {
        _options = options;
        _supervisor = supervisor;
        _updates = updates;
        _driver = driver;
        _language = language;
        _uiContext =
            SynchronizationContext.Current ?? throw new InvalidOperationException("Create the tray on the UI thread.");
        _autostart = new Autostart(Application.ExecutablePath);

        _autostartItem = new ToolStripMenuItem("", null, (_, _) => ToggleAutostart())
        {
            Checked = _autostart.IsEnabled,
        };
        var menu = new ContextMenuStrip();
        _openItem = new ToolStripMenuItem("", null, (_, _) => ShowMainForm())
        {
            Font = new Font(menu.Font, FontStyle.Bold),
        };
        menu.Items.Add(_openItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_autostartItem);
        if (_updates != null)
        {
            _notifiedVersion = _updates.PendingUpdate?.Version.ToString();
            _updateItem = new ToolStripMenuItem("", null, async (_, _) => await OnUpdateItemClickedAsync());
            menu.Items.Add(_updateItem);
            _updates.Changed += (_, _) => OnUpdatesChanged();
            _updates.Installing += (_, _) =>
            {
                _notifyIcon!.Text = Strings.StatusUpdating;
                _mainForm?.Close();
                _notifyIcon.Visible = false;
            };
            OnUpdatesChanged();
        }

        menu.Items.Add(new ToolStripSeparator());
        _closeItem = new ToolStripMenuItem("", null, (_, _) => ExitThread());
        menu.Items.Add(_closeItem);
        _exitItem = new ToolStripMenuItem("", null, async (_, _) => await ExitMicserAsync());
        menu.Items.Add(_exitItem);
        menu.ShowItemToolTips = true;

        _notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ShowMainForm();

        ApplyTexts();
        _language.Changed += (_, _) => ApplyTexts();
        _supervisor.Changed += (_, _) => UpdateStatus();
        _supervisor.Start();

        _activationRegistration = ThreadPool.RegisterWaitForSingleObject(
            activation,
            (_, _) => _uiContext.Post(_ => ShowMainForm(), null),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false
        );

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

    /// <summary>
    /// Sets the menu's and the icon's texts in the current language.
    /// </summary>
    private void ApplyTexts()
    {
        _openItem.Text = Strings.TrayOpen;
        _autostartItem.Text = Strings.TrayStartWithWindows;
        _closeItem.Text = Strings.TrayClose;
        _closeItem.ToolTipText = Strings.TrayCloseHint;
        _exitItem.Text = Strings.TrayExit;
        _exitItem.ToolTipText = Strings.TrayExitHint;
        if (_updates != null)
        {
            _updateItem!.ToolTipText = string.Format(
                _language.Culture,
                Strings.TrayInstalledVersion,
                _updates.CurrentVersion
            );
            UpdateUpdateItem();
        }

        UpdateStatus();
    }

    private async Task ExitMicserAsync()
    {
        _notifyIcon.Text = Strings.StatusStopping;
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
                _notifyIcon.ShowBalloonTip(
                    5000,
                    "Micser",
                    string.Format(_language.Culture, Strings.UpToDate, _updates.CurrentVersion),
                    ToolTipIcon.Info
                );
                break;
            case UpdateCheckResult.Failed:
                _notifyIcon.ShowBalloonTip(5000, "Micser", Strings.UpdateCheckFailed, ToolTipIcon.Warning);
                break;
        }
    }

    private void OnUpdatesChanged()
    {
        UpdateUpdateItem();

        // each new update is announced once, whether the check ran in the background or was requested
        var version = _updates!.PendingUpdate?.Version.ToString();
        if (version != null && version != _notifiedVersion)
        {
            _notifyIcon?.ShowBalloonTip(
                5000,
                string.Format(_language.Culture, Strings.UpdateReadyTitle, version),
                Strings.UpdateReadyText,
                ToolTipIcon.Info
            );
        }

        _notifiedVersion = version;
    }

    private void ShowMainForm()
    {
        if (_mainForm == null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(
                _supervisor,
                _updates,
                _driver,
                _language,
                _options.UiUrl,
                WindowSettings.DefaultPath
            );
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
        catch (Exception ex)
            when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
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
                string.Format(_language.Culture, Strings.DriverUpdateAvailable, status.BundledVersion),
                ToolTipIcon.Info
            );
        }
    }

    private void UpdateStatus()
    {
        _notifyIcon.Text = _supervisor.State switch
        {
            EngineState.Running => Strings.StatusRunning,
            EngineState.Starting => Strings.StatusStarting,
            EngineState.Paused => Strings.StatusPaused,
            _ => Strings.StatusNotRunning,
        };
    }

    private void UpdateUpdateItem()
    {
        var pending = _updates!.PendingUpdate;
        _updateItem!.Enabled = !_updates.IsChecking;
        _updateItem.Text =
            _updates.IsChecking ? Strings.TrayCheckingForUpdates
            : pending != null ? string.Format(_language.Culture, Strings.TrayRestartToUpdate, pending.Version)
            : Strings.TrayCheckForUpdates;
    }
}
