using Microsoft.Web.WebView2.WinForms;

namespace Micser.Shell;

/// <summary>
/// Hosts the web UI. Closing the window only hides the UI; the shell keeps running in the tray.
/// </summary>
internal sealed class MainForm : Form
{
    public MainForm(Uri url)
    {
        Text = "Micser";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ClientSize = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;

        var webView = new WebView2
        {
            Dock = DockStyle.Fill,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Micser", "WebView2"),
            },
            Source = url,
        };

        Controls.Add(webView);
    }
}
