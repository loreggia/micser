using Micser.Tests.Common;

namespace Micser.Shell.Tests;

public class DesktopIntegrationTests
{
    [Test]
    public async Task MenuEntry_SetEnabled_WritesAndRemovesTheEntryAndIcon()
    {
        using var folder = new TempFolder();
        var entry = folder.MenuEntry("/home/me/Apps/Micser.AppImage");

        var initially = entry.IsEnabled;
        entry.SetEnabled(true);
        var lines = await File.ReadAllLinesAsync(entry.DesktopEntryPath);
        var hasIcon = File.Exists(entry.IconPath);
        entry.SetEnabled(false);

        await Assert.That(initially).IsFalse();
        await Assert.That(lines).Contains("Exec=\"/home/me/Apps/Micser.AppImage\"");
        await Assert.That(lines).Contains("StartupWMClass=Micser");
        await Assert.That(hasIcon).IsTrue();
        await Assert.That(entry.IsEnabled).IsFalse();
        await Assert.That(File.Exists(entry.IconPath)).IsFalse();
    }

    [Test]
    public async Task FollowAppImage_WithoutEntries_SetsNothingUp()
    {
        using var folder = new TempFolder();
        var desktop = folder.Integration("/home/me/Apps/Micser.AppImage");

        desktop.FollowAppImage(autostartExists: false);

        await Assert.That(desktop.IsAutostartEnabled).IsFalse();
        await Assert.That(desktop.IsMenuEntryEnabled).IsFalse();
    }

    [Test]
    public async Task FollowAppImage_MovedAppImage_PointsTheEntriesToIt()
    {
        using var folder = new TempFolder();
        var old = folder.Integration("/home/me/Downloads/Micser.AppImage");
        old.SetAutostart(true);
        old.SetMenuEntry(true);
        var moved = folder.Integration("/home/me/Apps/Micser.AppImage");

        moved.FollowAppImage(autostartExists: true);

        await Assert.That(moved.IsAutostartEnabled).IsTrue();
        await Assert
            .That(await File.ReadAllLinesAsync(folder.MenuEntry("/home/me/Apps/Micser.AppImage").DesktopEntryPath))
            .Contains("Exec=\"/home/me/Apps/Micser.AppImage\"");
    }

    [Test]
    public async Task SetAutostart_RaisesChanged()
    {
        using var folder = new TempFolder();
        var desktop = folder.Integration("/home/me/Apps/Micser.AppImage");
        var changes = 0;
        desktop.Changed += (_, _) => changes++;

        desktop.SetAutostart(true);
        desktop.SetMenuEntry(true);

        await Assert.That(changes).IsEqualTo(2);
        await Assert.That(desktop.IsAutostartEnabled).IsTrue();
        await Assert.That(desktop.IsMenuEntryEnabled).IsTrue();
    }

    [Test]
    public async Task WithoutAppImage_HasNoMenuEntry()
    {
        using var folder = new TempFolder();
        var desktop = new DesktopIntegration(folder.Autostart("/opt/micser/Micser.Shell"), null);

        desktop.SetMenuEntry(true);

        await Assert.That(desktop.IsMenuEntryEnabled).IsNull();
    }

    private sealed class TempFolder : IDisposable
    {
        private readonly TempDirectory _directory = new();

        public XdgAutostart Autostart(string path)
        {
            return new XdgAutostart(path, _directory.Combine("config", "autostart", "micser.desktop"));
        }

        public void Dispose()
        {
            _directory.Dispose();
        }

        public DesktopIntegration Integration(string appImagePath)
        {
            return new DesktopIntegration(Autostart(appImagePath), MenuEntry(appImagePath));
        }

        public AppImageMenuEntry MenuEntry(string appImagePath)
        {
            return new AppImageMenuEntry(appImagePath, _directory.Combine("data"));
        }
    }
}
