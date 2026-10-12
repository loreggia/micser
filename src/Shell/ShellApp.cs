using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// The shell: a tray icon with its menu, notifications, and the main window, created on demand. It supervises the engine and runs
/// updates. Everything runs on the UI thread.
/// </summary>
internal sealed class ShellApp : Application
{
    private readonly ShellInstance _instance;
    private readonly ShellOptions _options;
    private IAutostart _autostart = null!;
    private NativeMenuItem _autostartItem = null!;
    private NativeMenuItem _closeItem = null!;
    private ShellColorMode _colorMode = null!;
    private DriverController? _driver;
    private NativeMenuItem _exitItem = null!;
    private HttpClient _http = null!;
    private ShellLanguage _language = null!;
    private IClassicDesktopStyleApplicationLifetime _lifetime = null!;
    private MainWindow? _mainWindow;
    private NativeMenuItem _openItem = null!;
    private EngineSupervisor _supervisor = null!;
    private TrayIcon _trayIcon = null!;
    private UpdateController? _updates;
    private NativeMenuItem? _updateItem;

    public ShellApp(ShellOptions options, ShellInstance instance)
    {
        _options = options;
        _instance = instance;
    }

    public override void Initialize()
    {
        Name = "Micser";
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _lifetime = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        _lifetime.Exit += (_, _) => Dispose();
        Dispatcher.UIThread.UnhandledException += OnUnhandledException;

        _colorMode = new ShellColorMode(ShellColorMode.DefaultPath);
        RequestedThemeVariant = _colorMode.ThemeVariant;
        ActualThemeVariantChanged += (_, _) => ApplyWebColorScheme();
        ApplyWebColorScheme();
        _language = new ShellLanguage(ShellLanguage.DefaultPath);
        _http = new HttpClient();
        var enginePath =
            _options.EnginePath
            ?? Path.Combine(
                AppContext.BaseDirectory,
                OperatingSystem.IsWindows() ? "Micser.Engine.exe" : "Micser.Engine"
            );
        _supervisor = new EngineSupervisor(
            new EngineLocator(EngineLocator.DefaultDiscoveryPath, _http),
            _http,
            enginePath
        );
        _updates = Updater.Create() is { } updater ? new UpdateController(updater, _supervisor) : null;
        _driver = DriverController.Create(_supervisor);
        var appImagePath = OperatingSystem.IsLinux() ? AppImageIntegration.CurrentAppImagePath : null;
        _autostart = Autostart.Create(appImagePath ?? Environment.ProcessPath!);
        if (appImagePath != null)
        {
            AppImageIntegration.Create(appImagePath).Apply(_autostart, File.Exists(XdgAutostart.DefaultPath));
        }

        CreateTrayIcon();
        _language.Changed += (_, _) => ApplyTexts();
        _colorMode.Changed += (_, _) =>
        {
            RequestedThemeVariant = _colorMode.ThemeVariant;
            _mainWindow?.ApplyColorMode();
        };
        _supervisor.Changed += (_, _) => UpdateStatus();
        _supervisor.Start();
        _instance.ActivationRequested += (_, _) => Dispatcher.UIThread.Post(ShowMainWindow);

        if (!_options.StartMinimized)
        {
            ShowMainWindow();
        }

        _updates?.Start();
        _ = RefreshDriverAsync();
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Sets the menu's and the icon's texts in the current language.
    /// </summary>
    private void ApplyTexts()
    {
        _openItem.Header = Strings.TrayOpen;
        _autostartItem.Header = OperatingSystem.IsWindows() ? Strings.TrayStartWithWindows : Strings.TrayStartAtLogin;
        _closeItem.Header = Strings.TrayClose;
        _closeItem.ToolTip = Strings.TrayCloseHint;
        _exitItem.Header = Strings.TrayExit;
        _exitItem.ToolTip = Strings.TrayExitHint;
        if (_updates != null)
        {
            _updateItem!.ToolTip = string.Format(
                _language.Culture,
                Strings.TrayInstalledVersion,
                _updates.CurrentVersion
            );
            UpdateUpdateItem();
        }

        UpdateStatus();
    }

    /// <summary>
    /// On Linux, makes the web view light or dark like the app: the color mode preference, otherwise the desktop's. WebView2 gets it from
    /// the main window.
    /// </summary>
    private void ApplyWebColorScheme()
    {
        if (OperatingSystem.IsLinux())
        {
            _ = GtkColorScheme.ApplyAsync(ActualThemeVariant == ThemeVariant.Dark);
        }
    }

    private void CreateTrayIcon()
    {
        _openItem = new NativeMenuItem();
        _openItem.Click += (_, _) => ShowMainWindow();
        _autostartItem = new NativeMenuItem
        {
            ToggleType = MenuItemToggleType.CheckBox,
            IsChecked = _autostart.IsEnabled,
        };
        _autostartItem.Click += (_, _) => ToggleAutostart();
        var menu = new NativeMenu();
        menu.Items.Add(_openItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(_autostartItem);
        if (_updates != null)
        {
            _updateItem = new NativeMenuItem();
            _updateItem.Click += async (_, _) => await OnUpdateItemClickedAsync();
            menu.Items.Add(_updateItem);
            _updates.Changed += (_, _) => UpdateUpdateItem();
            _updates.Installing += (_, _) =>
            {
                _trayIcon.ToolTipText = Strings.StatusUpdating;
                _mainWindow?.Close();
                _trayIcon.IsVisible = false;
            };
        }

        menu.Items.Add(new NativeMenuItemSeparator());
        _closeItem = new NativeMenuItem();
        _closeItem.Click += (_, _) => _lifetime.Shutdown();
        menu.Items.Add(_closeItem);
        _exitItem = new NativeMenuItem();
        _exitItem.Click += async (_, _) => await ExitMicserAsync();
        menu.Items.Add(_exitItem);

        using var icon = ShellIcons.OpenIcon();
        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(icon),
            Menu = menu,
            IsVisible = true,
        };
        _trayIcon.Clicked += (_, _) => ShowMainWindow();
        TrayIcon.SetIcons(this, [_trayIcon]);

        ApplyTexts();
    }

    private void Dispose()
    {
        _trayIcon.IsVisible = false;
        _trayIcon.Dispose();
        _updates?.Dispose();
        _supervisor.Dispose();
        _http.Dispose();
    }

    private async Task ExitMicserAsync()
    {
        _trayIcon.ToolTipText = Strings.StatusStopping;
        _mainWindow?.Close();
        await _supervisor.ShutdownEngineAsync();
        _lifetime.Shutdown();
    }

    private async Task OnUpdateItemClickedAsync()
    {
        if (_updates!.PendingUpdate != null)
        {
            await _updates.InstallAsync(minimized: _mainWindow == null);
            return;
        }

        await _updates.CheckAsync();
    }

    private void OnUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "The shell failed.");
        e.Handled = true;
        _ = ShowFatalErrorAsync(e.Exception);
    }

    private async Task ShowFatalErrorAsync(Exception exception)
    {
        await Dialogs.ShowAsync(null, string.Format(Strings.Culture, Strings.FatalError, exception.Message));
        _lifetime.Shutdown(1);
    }

    private async Task RefreshDriverAsync()
    {
        if (_driver != null)
        {
            await _driver.RefreshAsync();
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow(
                _supervisor,
                _updates,
                _driver,
                _language,
                _colorMode,
                _options.UiUrl,
                WindowSettings.DefaultPath
            );
            _mainWindow.Closed += (_, _) => _mainWindow = null;
        }

        _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
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

        _autostartItem.IsChecked = _autostart.IsEnabled;
    }

    private void UpdateStatus()
    {
        _trayIcon.ToolTipText = _supervisor.State switch
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
        _updateItem!.IsEnabled = !_updates.IsChecking;
        _updateItem.Header =
            _updates.IsChecking ? Strings.TrayCheckingForUpdates
            : pending != null ? string.Format(_language.Culture, Strings.TrayRestartToUpdate, pending.Version)
            : Strings.TrayCheckForUpdates;
    }
}
