namespace Micser.Shell;

/// <summary>
/// Keeps the shell alive in the notification area; the main window is created on demand.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Uri _url;
    private MainForm? _mainForm;

    public TrayApplicationContext(Uri url)
    {
        _url = url;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowMainForm());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        _notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            Text = "Micser",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ShowMainForm();

        ShowMainForm();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _mainForm?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ShowMainForm()
    {
        if (_mainForm == null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(_url);
        }

        _mainForm.Show();
        _mainForm.Activate();
    }
}
