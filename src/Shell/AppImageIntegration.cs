using Serilog;

namespace Micser.Shell;

/// <summary>
/// What an installer does on Windows, for the shell running as an AppImage (Linux), which has no install step: a menu entry with the icon,
/// and autostart on the first start. Both point to the AppImage file (<c>$APPIMAGE</c>), since the shell itself runs from a mount that
/// changes with every start. Applied on every start, so they follow the AppImage when it's moved.
/// </summary>
internal sealed class AppImageIntegration
{
    private readonly string _appImagePath;
    private readonly string _dataHome;
    private readonly string _markerPath;

    /// <param name="appImagePath">The AppImage file.</param>
    /// <param name="dataHome">The user's data folder (<c>$XDG_DATA_HOME</c>, <c>~/.local/share</c>), for the menu entry and the icon.</param>
    /// <param name="markerPath">A file that marks that the first start is done.</param>
    public AppImageIntegration(string appImagePath, string dataHome, string markerPath)
    {
        _appImagePath = appImagePath;
        _dataHome = dataHome;
        _markerPath = markerPath;
    }

    /// <summary>
    /// The AppImage file the shell runs from, or null if it doesn't run from one.
    /// </summary>
    public static string? CurrentAppImagePath =>
        Environment.GetEnvironmentVariable("APPIMAGE") is { Length: > 0 } path ? path : null;

    public string DesktopEntryPath => Path.Combine(_dataHome, "applications", "micser.desktop");

    public string IconPath => Path.Combine(_dataHome, "icons", "hicolor", "256x256", "apps", "micser.png");

    public static AppImageIntegration Create(string appImagePath)
    {
        // on Linux, LocalApplicationData is $XDG_DATA_HOME or ~/.local/share
        var dataHome = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new AppImageIntegration(appImagePath, dataHome, Path.Combine(dataHome, "Micser", "appimage-installed"));
    }

    /// <summary>
    /// Writes the menu entry and the icon, turns autostart on at the first start and keeps an existing autostart entry pointing to this
    /// AppImage.
    /// </summary>
    /// <param name="autostart">The autostart entry for the AppImage file.</param>
    /// <param name="autostartExists">Whether an autostart entry exists, maybe for another path.</param>
    public void Apply(IAutostart autostart, bool autostartExists)
    {
        try
        {
            WriteIfChanged(DesktopEntryPath, DesktopEntry());
            WriteIconIfChanged();

            var isFirstStart = !File.Exists(_markerPath);
            if ((isFirstStart || autostartExists) && !autostart.IsEnabled)
            {
                autostart.SetEnabled(true);
            }

            if (isFirstStart)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_markerPath)!);
                File.WriteAllText(_markerPath, "");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error(ex, "Setting up the menu entry or autostart failed.");
        }
    }

    private static void WriteIfChanged(string path, string text)
    {
        if (File.Exists(path) && File.ReadAllText(path) == text)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    private string DesktopEntry()
    {
        return $"""
            [Desktop Entry]
            Type=Application
            Name=Micser
            GenericName=Audio router
            Comment=Routes audio between devices and applications
            Exec={XdgAutostart.Quote(_appImagePath)}
            Icon=micser
            Terminal=false
            Categories=AudioVideo;Audio;
            StartupWMClass=Micser

            """;
    }

    private void WriteIconIfChanged()
    {
        using var stream = ShellIcons.OpenIcon();
        using var icon = new MemoryStream();
        stream.CopyTo(icon);
        var bytes = icon.ToArray();
        if (File.Exists(IconPath) && File.ReadAllBytes(IconPath).AsSpan().SequenceEqual(bytes))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(IconPath)!);
        File.WriteAllBytes(IconPath, bytes);
    }
}
