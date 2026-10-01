using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// Shows the web UI of the engine. Closing the window disposes it (and its WebView2 processes); the shell keeps
/// running in the tray. The UI talks to the shell through WebView2 web messages (see <see cref="OnWebMessageReceived"/>).
/// </summary>
internal sealed class MainForm : Form
{
    private const string WebView2DownloadUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

    private static readonly JsonSerializerOptions MessageJson = new(JsonSerializerDefaults.Web);

    private readonly string _settingsPath;
    private readonly EngineSupervisor _supervisor;
    private readonly Uri? _uiUrl;
    private readonly UpdateController? _updates;
    private readonly WebView2 _webView;
    private Uri? _shownUrl;

    /// <param name="updates">Null if this copy can't update (development).</param>
    public MainForm(EngineSupervisor supervisor, UpdateController? updates, Uri? uiUrl, string settingsPath)
    {
        _supervisor = supervisor;
        _updates = updates;
        _uiUrl = uiUrl;
        _settingsPath = settingsPath;

        Text = "Micser";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        MinimumSize = new Size(640, 400);
        ApplyWindowSettings(WindowSettings.Load(settingsPath));

        _webView = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.Transparent };
        Controls.Add(_webView);

        _supervisor.Changed += OnEngineChanged;
        if (_updates != null)
        {
            _updates.Changed += OnUpdatesChanged;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _supervisor.Changed -= OnEngineChanged;
            if (_updates != null)
            {
                _updates.Changed -= OnUpdatesChanged;
            }
        }

        base.Dispose(disposing);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        new WindowSettings(bounds.X, bounds.Y, bounds.Width, bounds.Height, WindowState == FormWindowState.Maximized).Save(_settingsPath);
        base.OnFormClosing(e);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        try
        {
            var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Micser", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await _webView.EnsureCoreWebView2Async(environment);
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            Log.Error(ex, "The WebView2 runtime is missing.");
            var answer = MessageBox.Show(
                this,
                "Micser needs the Microsoft Edge WebView2 Runtime to show its window. Open the download page?",
                "Micser",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (answer == DialogResult.Yes)
            {
                OpenInBrowser(new Uri(WebView2DownloadUrl));
            }

            Close();
            return;
        }

        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        _webView.CoreWebView2.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri))
            {
                OpenInBrowser(uri);
            }
        };

        ShowEngine();
    }

    private static void OpenInBrowser(Uri uri)
    {
        if (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();
        }
    }

    private void ApplyWindowSettings(WindowSettings? settings)
    {
        if (settings != null && settings.IsVisibleOn(Screen.AllScreens.Select(s => s.WorkingArea)))
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = new Rectangle(settings.X, settings.Y, settings.Width, settings.Height);
            WindowState = settings.IsMaximized ? FormWindowState.Maximized : FormWindowState.Normal;
        }
        else
        {
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1280, 800);
        }
    }

    private void OnEngineChanged(object? sender, EventArgs e)
    {
        ShowEngine();
        PostState();
    }

    private void OnUpdatesChanged(object? sender, EventArgs e)
    {
        PostState();
    }

    /// <summary>
    /// Handles <c>{ "type": ... }</c> messages from the UI: <c>getState</c>, <c>checkForUpdates</c> (answered with an
    /// <c>updateCheck</c> message), <c>installUpdate</c> and <c>restartEngine</c>. The shell answers with <c>state</c> messages (see
    /// <see cref="PostState"/>), also whenever the state changes.
    /// </summary>
    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // only the UI the window loaded may control the shell
        if (_shownUrl == null || !Uri.TryCreate(e.Source, UriKind.Absolute, out var source) || source.GetLeftPart(UriPartial.Authority) != _shownUrl.GetLeftPart(UriPartial.Authority))
        {
            return;
        }

        string? type;
        try
        {
            type = JsonDocument.Parse(e.WebMessageAsJson).RootElement.GetProperty("type").GetString();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            Log.Warning(ex, "Ignoring an invalid message from the UI.");
            return;
        }

        switch (type)
        {
            case "getState":
                PostState();
                break;
            case "checkForUpdates" when _updates != null:
                var result = await _updates.CheckAsync();
                PostMessage(new { type = "updateCheck", result = JsonNamingPolicy.CamelCase.ConvertName(result.ToString()) });
                break;
            case "installUpdate" when _updates != null:
                await _updates.InstallAsync(minimized: false);
                break;
            case "restartEngine":
                await _supervisor.RestartEngineAsync();
                break;
        }
    }

    private void PostMessage(object message)
    {
        if (_webView.CoreWebView2 != null && _shownUrl != null)
        {
            _webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, MessageJson));
        }
    }

    /// <summary>
    /// Sends <c>{ type: "state", version, canUpdate, isCheckingForUpdates, pendingUpdate, canRestartEngine }</c> to the UI.
    /// </summary>
    private void PostState()
    {
        PostMessage(new
        {
            type = "state",
            version = _updates?.CurrentVersion.ToString(),
            canUpdate = _updates != null,
            isCheckingForUpdates = _updates?.IsChecking ?? false,
            pendingUpdate = _updates?.PendingUpdate?.Version.ToString(),
            canRestartEngine = _supervisor.CanStartEngine,
        });
    }

    /// <summary>
    /// Shows the UI of the current engine with its access token, or a waiting page.
    /// </summary>
    private void ShowEngine()
    {
        if (_webView.CoreWebView2 == null)
        {
            return;
        }

        var engine = _supervisor.Engine;
        if (engine == null)
        {
            _shownUrl = null;
            _webView.NavigateToString(StatusPage(_supervisor.State));
            return;
        }

        var url = new UriBuilder(_uiUrl ?? engine.Url);
        if (engine.Token != null)
        {
            url.Fragment = "token=" + Uri.EscapeDataString(engine.Token);
        }

        // the token changes with every engine start, so a changed URL means a new engine
        if (url.Uri != _shownUrl)
        {
            _shownUrl = url.Uri;
            _webView.CoreWebView2.Navigate(url.Uri.AbsoluteUri);
        }
    }

    private static string StatusPage(EngineState state)
    {
        var message = state == EngineState.Unavailable
            ? "The audio engine isn't running and couldn't be started. Its log is in %LOCALAPPDATA%\\Micser\\logs."
            : "Starting the audio engine…";

        return $$"""
            <!doctype html>
            <html>
            <head>
            <meta charset="utf-8">
            <style>
              :root { color-scheme: light dark; }
              body { margin: 0; height: 100vh; display: grid; place-items: center; font: 14px "Segoe UI", sans-serif; }
            </style>
            </head>
            <body><p>{{WebUtility.HtmlEncode(message)}}</p></body>
            </html>
            """;
    }
}
