using Serilog;

namespace Micser.Shell;

/// <summary>
/// The shell's entries in the desktop, which the user switches in the tray menu (autostart) and in the settings: autostart, and for an
/// AppImage the app menu entry. <see cref="Changed"/> keeps the tray and the settings in sync. Nothing is set up on its own (the Windows
/// installer turns autostart on, see <see cref="InstallHooks"/>).
/// </summary>
internal sealed class DesktopIntegration
{
    private readonly IAutostart _autostart;
    private readonly AppImageMenuEntry? _menuEntry;

    /// <param name="menuEntry">Null if the shell doesn't run as an AppImage.</param>
    public DesktopIntegration(IAutostart autostart, AppImageMenuEntry? menuEntry)
    {
        _autostart = autostart;
        _menuEntry = menuEntry;
    }

    public event EventHandler? Changed;

    public bool IsAutostartEnabled => _autostart.IsEnabled;

    /// <summary>
    /// Null if there's no menu entry to switch (not an AppImage).
    /// </summary>
    public bool? IsMenuEntryEnabled => _menuEntry?.IsEnabled;

    /// <summary>
    /// The entries for this shell: an AppImage's point to the AppImage file, since the shell runs from a mount that changes with every
    /// start. Entries of an AppImage that was moved follow it.
    /// </summary>
    public static DesktopIntegration Create()
    {
        var appImagePath = OperatingSystem.IsLinux() ? AppImageMenuEntry.CurrentAppImagePath : null;
        var integration = new DesktopIntegration(
            Autostart.Create(appImagePath ?? Environment.ProcessPath!),
            appImagePath == null ? null : AppImageMenuEntry.Create(appImagePath)
        );
        if (appImagePath != null)
        {
            integration.FollowAppImage(File.Exists(XdgAutostart.DefaultPath));
        }

        return integration;
    }

    /// <summary>
    /// Points the entries that exist to this AppImage, e.g. after it was moved.
    /// </summary>
    /// <param name="autostartExists">Whether an autostart entry exists, maybe for another path.</param>
    public void FollowAppImage(bool autostartExists)
    {
        Change(() =>
        {
            if (autostartExists && !_autostart.IsEnabled)
            {
                _autostart.SetEnabled(true);
            }

            _menuEntry?.Refresh();
        });
    }

    public void SetAutostart(bool enabled)
    {
        Change(() => _autostart.SetEnabled(enabled));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetMenuEntry(bool enabled)
    {
        if (_menuEntry != null)
        {
            Change(() => _menuEntry.SetEnabled(enabled));
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private static void Change(Action change)
    {
        try
        {
            change();
        }
        catch (Exception ex)
            when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            Log.Error(ex, "Changing an autostart or menu entry failed.");
        }
    }
}
