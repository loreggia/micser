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
    private readonly EngineSupervisor _supervisor;
    private readonly SynchronizationContext _uiContext;
    private MainForm? _mainForm;

    /// <param name="activation">Signaled by another shell instance to show the window.</param>
    public TrayApplicationContext(ShellOptions options, EngineSupervisor supervisor, EventWaitHandle activation)
    {
        _options = options;
        _supervisor = supervisor;
        _uiContext = SynchronizationContext.Current ?? throw new InvalidOperationException("Create the tray on the UI thread.");
        _autostart = new Autostart(Application.ExecutablePath);

        _autostartItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleAutostart()) { Checked = _autostart.IsEnabled };
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Open", null, (_, _) => ShowMainForm()) { Font = new Font(menu.Font, FontStyle.Bold) });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_autostartItem);
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
