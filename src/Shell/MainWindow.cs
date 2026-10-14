using System.Diagnostics;
using System.Net;
using System.Runtime.Versioning;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Styling;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// Shows the web UI of the engine in the platform's web view (WebView2 on Windows, WebKitGTK on Linux). Closing the window disposes it
/// and its web view processes; the shell keeps running in the tray. The UI talks to the shell with <c>invokeCSharpAction(json)</c>, and
/// the shell answers through <c>window.micserShell.receive(message)</c> (see <see cref="OnWebMessageReceived"/>).
/// </summary>
internal sealed class MainWindow : Window
{
    private const string WebView2DownloadUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

    private static readonly JsonSerializerOptions MessageJson = new(JsonSerializerDefaults.Web);

    private readonly ShellColorMode _colorMode;
    private readonly DesktopIntegration _desktop;
    private readonly DriverController? _driver;
    private readonly string _settingsPath;
    private readonly ShellLanguage _language;
    private readonly EngineSupervisor _supervisor;
    private readonly Uri? _uiUrl;
    private readonly UpdateController? _updates;
    private readonly NativeWebView _webView;
    private PixelRect _normalBounds;
    private Uri? _shownUrl;
    private CoreWebView2? _webView2;

    /// <param name="updates">Null if this copy can't update (development).</param>
    /// <param name="driver">Null if this copy has no driver package.</param>
    public MainWindow(
        EngineSupervisor supervisor,
        UpdateController? updates,
        DriverController? driver,
        DesktopIntegration desktop,
        ShellLanguage language,
        ShellColorMode colorMode,
        Uri? uiUrl,
        string settingsPath
    )
    {
        _supervisor = supervisor;
        _updates = updates;
        _driver = driver;
        _desktop = desktop;
        _language = language;
        _colorMode = colorMode;
        _uiUrl = uiUrl;
        _settingsPath = settingsPath;

        Title = "Micser";
        ShellIcons.SetIcon(this);
        MinWidth = 640;
        MinHeight = 400;
        Width = 1280;
        Height = 800;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _webView = new NativeWebView();
        _webView.EnvironmentRequested += OnEnvironmentRequested;
        _webView.AdapterCreated += OnAdapterCreated;
        _webView.WebMessageReceived += OnWebMessageReceived;
        // the UI's getState can arrive before the web view reports the new source, which drops it, so the loaded page gets the state too
        _webView.NavigationCompleted += (_, _) => PostState();
        _webView.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            if (e.Request != null)
            {
                OpenInBrowser(e.Request);
            }
        };
        Content = _webView;

        _supervisor.Changed += OnEngineChanged;
        _updates?.Changed += OnStateChanged;
        _driver?.Changed += OnStateChanged;
        _desktop.Changed += OnStateChanged;
        Opened += (_, _) => ApplyWindowSettings(WindowSettings.Load(settingsPath));
        PositionChanged += (_, _) => RememberNormalBounds();
        Resized += (_, _) => RememberNormalBounds();
    }

    /// <summary>
    /// Applies the color mode to the window and the web view.
    /// </summary>
    public void ApplyColorMode()
    {
        RequestedThemeVariant = _colorMode.ThemeVariant;
        if (OperatingSystem.IsWindows())
        {
            ApplyWebView2ColorScheme();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _supervisor.Changed -= OnEngineChanged;
        _updates?.Changed -= OnStateChanged;
        _driver?.Changed -= OnStateChanged;
        _desktop.Changed -= OnStateChanged;
        base.OnClosed(e);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        RememberNormalBounds();
        var bounds = _normalBounds;
        if (bounds.Width > 0)
        {
            new WindowSettings(
                bounds.X,
                bounds.Y,
                bounds.Width,
                bounds.Height,
                WindowState == WindowState.Maximized
            ).Save(_settingsPath);
        }

        base.OnClosing(e);
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        RequestedThemeVariant = _colorMode.ThemeVariant;

        if (OperatingSystem.IsWindows() && !IsWebView2Available())
        {
            if (await Dialogs.ShowAsync(this, Strings.WebView2Missing, yesNo: true))
            {
                OpenInBrowser(new Uri(WebView2DownloadUrl));
            }

            Close();
            return;
        }

        ShowEngine();
    }

    [SupportedOSPlatform("windows")]
    private static bool IsWebView2Available()
    {
        try
        {
            return CoreWebView2Environment.GetAvailableBrowserVersionString() != null;
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            Log.Error(ex, "The WebView2 runtime is missing.");
            return false;
        }
    }

    private static void OpenInBrowser(Uri uri)
    {
        if (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();
        }
    }

    private static string StatusPage(EngineState state)
    {
        var message = state switch
        {
            EngineState.Unavailable => Strings.PageEngineUnavailable,
            EngineState.Paused => Strings.PageChangingCables,
            _ => Strings.PageStarting,
        };

        return $$"""
            <!doctype html>
            <html lang="{{Strings.Culture?.TwoLetterISOLanguageName ?? "en"}}">
            <head>
            <meta charset="utf-8">
            <style>
              :root { color-scheme: light dark; background: Canvas; color: CanvasText; }
              body { margin: 0; height: 100vh; display: grid; place-items: center; font: 14px "Segoe UI", sans-serif; }
            </style>
            </head>
            <body><p>{{WebUtility.HtmlEncode(message)}}</p></body>
            </html>
            """;
    }

    /// <summary>
    /// The WebView2 settings that Avalonia's web view doesn't have: the color scheme and no default context menu.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void ApplyWebView2ColorScheme()
    {
        _webView2?.Profile.PreferredColorScheme =
            _colorMode.ThemeVariant == ThemeVariant.Light ? CoreWebView2PreferredColorScheme.Light
            : _colorMode.ThemeVariant == ThemeVariant.Dark ? CoreWebView2PreferredColorScheme.Dark
            : CoreWebView2PreferredColorScheme.Auto;
    }

    private void ApplyWindowSettings(WindowSettings? settings)
    {
        if (settings == null || Screens.All.Count == 0)
        {
            return;
        }

        if (settings.IsVisibleOn(Screens.All.Select(s => ToRectangle(s.WorkingArea))))
        {
            Position = new PixelPoint(settings.X, settings.Y);
            var scaling = Screens.ScreenFromPoint(Position)?.Scaling ?? 1;
            Width = settings.Width / scaling;
            Height = settings.Height / scaling;
            if (settings.IsMaximized)
            {
                WindowState = WindowState.Maximized;
            }
        }
    }

    private void OnAdapterCreated(object? sender, WebViewAdapterEventArgs e)
    {
        if (OperatingSystem.IsWindows() && e.TryGetPlatformHandle() is IWindowsWebView2PlatformHandle handle)
        {
            ConfigureWebView2(handle);
        }
    }

    [SupportedOSPlatform("windows")]
    private void ConfigureWebView2(IWindowsWebView2PlatformHandle handle)
    {
        _webView2 = CoreWebView2.CreateFromComICoreWebView2(handle.CoreWebView2);
        _webView2.Settings.AreDefaultContextMenusEnabled = false;
        ApplyWebView2ColorScheme();
    }

    private void OnEngineChanged(object? sender, EventArgs e)
    {
        ShowEngine();
        PostState();
    }

    private void OnEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e)
    {
        var dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Micser"
        );
        switch (e)
        {
            case WindowsWebView2EnvironmentRequestedEventArgs webView2:
                webView2.UserDataFolder = Path.Combine(dataFolder, "WebView2");
                break;
            case GtkWebViewEnvironmentRequestedEventArgs gtk:
                gtk.BaseDataDirectory = Path.Combine(dataFolder, "webkit");
                gtk.BaseCacheDirectory = Path.Combine(
                    Environment.GetEnvironmentVariable("XDG_CACHE_HOME") is { Length: > 0 } cache
                        ? cache
                        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache"),
                    "Micser",
                    "webkit"
                );
                break;
        }
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        PostState();
    }

    /// <summary>
    /// Handles <c>{ "type": ... }</c> messages from the UI: <c>getState</c>, <c>checkForUpdates</c> (answered with an
    /// <c>updateCheck</c> message), <c>installUpdate</c>, <c>restartEngine</c>, and for the driver <c>installDriver</c> and
    /// <c>setCableCount</c> (with <c>cableCount</c>), <c>setCableLayout</c> (with <c>cable</c> and <c>layout</c>), <c>updateDriver</c>
    /// and <c>uninstallDriver</c>, <c>setLanguage</c> (with the UI's <c>language</c> preference, or null), <c>setColorMode</c> (with the
    /// <c>colorMode</c> of the UI's theme preference, or null), and <c>getReleaseNotes</c> (with a <c>version</c>; answered with a
    /// <c>releaseNotes</c> message with the <c>version</c> and its markdown <c>notes</c>, or null), <c>setAutostart</c> and
    /// <c>setMenuEntry</c> (with <c>enabled</c>). The shell answers with <c>state</c>
    /// messages (see <see cref="PostState"/>), also whenever the state changes.
    /// </summary>
    private async void OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        // only the UI the window loaded may control the shell; the message has no origin, so the shown page's counts
        if (
            _shownUrl == null
            || _webView.Source is not { } source
            || source.GetLeftPart(UriPartial.Authority) != _shownUrl.GetLeftPart(UriPartial.Authority)
        )
        {
            return;
        }

        string? type;
        int cableCount;
        int cable;
        string? layout;
        string? language;
        string? colorMode;
        string? version;
        bool enabled;
        try
        {
            var message = JsonDocument.Parse(e.Body ?? "").RootElement;
            type = message.GetProperty("type").GetString();
            cableCount =
                message.TryGetProperty("cableCount", out var count) && count.TryGetInt32(out var value) ? value : 1;
            cable =
                message.TryGetProperty("cable", out var cableValue) && cableValue.TryGetInt32(out var number)
                    ? number
                    : 0;
            layout = GetString(message, "layout");
            language = GetString(message, "language");
            colorMode = GetString(message, "colorMode");
            version = GetString(message, "version");
            enabled =
                message.TryGetProperty("enabled", out var enabledValue) && enabledValue.ValueKind == JsonValueKind.True;
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
                PostMessage(
                    new { type = "updateCheck", result = JsonNamingPolicy.CamelCase.ConvertName(result.ToString()) }
                );
                break;
            case "installUpdate" when _updates != null:
                await _updates.InstallAsync(minimized: false);
                break;
            case "restartEngine":
                await _supervisor.RestartEngineAsync();
                break;
            case "installDriver" when _driver != null:
                await ShowDriverResultAsync(await _driver.InstallAsync(cableCount));
                break;
            case "setCableCount" when _driver != null:
                await ShowDriverResultAsync(await _driver.SetCableCountAsync(cableCount));
                break;
            case "setCableLayout" when _driver != null && cable > 0 && layout != null:
                await ShowDriverResultAsync(await _driver.SetCableLayoutAsync(cable, layout));
                break;
            case "updateDriver" when _driver != null:
                await ShowDriverResultAsync(await _driver.UpdateAsync());
                break;
            case "uninstallDriver" when _driver != null:
                await ShowDriverResultAsync(await _driver.UninstallAsync());
                break;
            case "setAutostart" when _updates != null:
                _desktop.SetAutostart(enabled);
                break;
            case "setMenuEntry":
                _desktop.SetMenuEntry(enabled);
                break;
            case "setLanguage":
                _language.SetPreference(language);
                break;
            case "setColorMode":
                _colorMode.SetPreference(colorMode);
                break;
            case "getReleaseNotes" when version != null:
                PostMessage(
                    new
                    {
                        type = "releaseNotes",
                        version,
                        notes = _updates?.GetReleaseNotes(version),
                    }
                );
                break;
        }

        static string? GetString(JsonElement message, string name)
        {
            return message.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
    }

    private void PostMessage(object message)
    {
        if (_shownUrl != null)
        {
            // JSON is a JavaScript expression, so the serialized message is passed as it is
            var json = JsonSerializer.Serialize(message, MessageJson);
            _ = _webView.InvokeScript($"window.micserShell && window.micserShell.receive({json})");
        }
    }

    /// <summary>
    /// Sends <c>{ type: "state", version, canUpdate, isCheckingForUpdates, pendingUpdate, canRestartEngine, driver, autostart,
    /// menuEntry }</c> to the UI. <c>driver</c> is null without a driver package, else <c>{ isBusy, status }</c> with the utility's status
    /// (or null). <c>autostart</c> and <c>menuEntry</c> are whether they're on, or null if the settings don't offer them.
    /// </summary>
    private void PostState()
    {
        PostMessage(
            new
            {
                type = "state",
                version = _updates?.CurrentVersion.ToString(),
                canUpdate = _updates != null,
                isCheckingForUpdates = _updates?.IsChecking ?? false,
                pendingUpdate = _updates?.PendingUpdate?.Version.ToString(),
                canRestartEngine = _supervisor.CanStartEngine,
                driver = _driver == null ? null : new { isBusy = _driver.IsBusy, status = _driver.Status },
                // the settings offer autostart in an installed copy only, like the other controls of the shell
                autostart = _updates == null ? (bool?)null : _desktop.IsAutostartEnabled,
                menuEntry = _desktop.IsMenuEntryEnabled,
            }
        );
    }

    /// <summary>
    /// The bounds to restore: the window's while it's neither maximized nor minimized, in pixels.
    /// </summary>
    private void RememberNormalBounds()
    {
        if (WindowState == WindowState.Normal && IsVisible)
        {
            var scaling = RenderScaling;
            _normalBounds = new PixelRect(
                Position,
                new PixelSize((int)Math.Round(Width * scaling), (int)Math.Round(Height * scaling))
            );
        }
    }

    private async Task ShowDriverResultAsync(DriverCommandResult result)
    {
        var text = result switch
        {
            DriverCommandResult.RebootRequired => Strings.DriverRebootRequired,
            DriverCommandResult.Failed => Strings.DriverFailed,
            _ => null,
        };

        if (text != null)
        {
            await Dialogs.ShowAsync(this, text);
        }
    }

    /// <summary>
    /// Shows the UI of the current engine with its access token, or a waiting page.
    /// </summary>
    private void ShowEngine()
    {
        var engine = _supervisor.Engine;
        if (engine == null)
        {
            _shownUrl = null;
            _webView.NavigateToString(StatusPage(_supervisor.State), null);
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
            _webView.Navigate(url.Uri);
        }
    }

    private static System.Drawing.Rectangle ToRectangle(PixelRect rect)
    {
        return new System.Drawing.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
    }
}
