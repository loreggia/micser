namespace Micser.Shell;

/// <summary>
/// The app menu entry of the shell running as an AppImage (Linux), which has no install step: a desktop entry in
/// <c>~/.local/share/applications</c> and its icon, pointing to the AppImage file (<c>$APPIMAGE</c>) rather than to the mount the shell runs
/// from, which changes with every start.
/// </summary>
internal sealed class AppImageMenuEntry
{
    private readonly string _appImagePath;
    private readonly string _dataHome;

    /// <param name="appImagePath">The AppImage file.</param>
    /// <param name="dataHome">The user's data folder (<c>$XDG_DATA_HOME</c>, <c>~/.local/share</c>).</param>
    public AppImageMenuEntry(string appImagePath, string dataHome)
    {
        _appImagePath = appImagePath;
        _dataHome = dataHome;
    }

    /// <summary>
    /// The AppImage file the shell runs from, or null if it doesn't run from one.
    /// </summary>
    public static string? CurrentAppImagePath =>
        Environment.GetEnvironmentVariable("APPIMAGE") is { Length: > 0 } path ? path : null;

    public string DesktopEntryPath => Path.Combine(_dataHome, "applications", "micser.desktop");

    public string IconPath => Path.Combine(_dataHome, "icons", "hicolor", "256x256", "apps", "micser.png");

    /// <summary>
    /// Whether the entry exists (for this AppImage or another path).
    /// </summary>
    public bool IsEnabled => File.Exists(DesktopEntryPath);

    public static AppImageMenuEntry Create(string appImagePath)
    {
        // on Linux, LocalApplicationData is $XDG_DATA_HOME or ~/.local/share
        return new AppImageMenuEntry(
            appImagePath,
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        );
    }

    /// <summary>
    /// Points an existing entry to this AppImage, e.g. after it was moved.
    /// </summary>
    public void Refresh()
    {
        if (IsEnabled)
        {
            SetEnabled(true);
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            File.Delete(DesktopEntryPath);
            File.Delete(IconPath);
            return;
        }

        WriteIfChanged(DesktopEntryPath, DesktopEntry());
        WriteIconIfChanged();
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
        // StartupWMClass: the window class (Program), so the desktop puts the window under this entry
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
