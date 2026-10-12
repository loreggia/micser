namespace Micser.Shell.Tests;

public class AppImageIntegrationTests
{
    [Test]
    public async Task Apply_FirstStart_WritesTheMenuEntryAndTurnsAutostartOn()
    {
        using var folder = new TempFolder();
        var integration = folder.Integration("/home/me/Apps/Micser.AppImage");
        var autostart = folder.Autostart("/home/me/Apps/Micser.AppImage");

        integration.Apply(autostart, autostartExists: false);

        var entry = await File.ReadAllLinesAsync(integration.DesktopEntryPath);
        await Assert.That(entry).Contains("Exec=\"/home/me/Apps/Micser.AppImage\"");
        await Assert.That(entry).Contains("StartupWMClass=Micser");
        await Assert.That(File.Exists(integration.IconPath)).IsTrue();
        await Assert.That(autostart.IsEnabled).IsTrue();
    }

    [Test]
    public async Task Apply_LaterStart_LeavesAutostartOff()
    {
        using var folder = new TempFolder();
        var autostart = folder.Autostart("/home/me/Apps/Micser.AppImage");
        folder.Integration("/home/me/Apps/Micser.AppImage").Apply(autostart, autostartExists: false);
        autostart.SetEnabled(false);

        folder.Integration("/home/me/Apps/Micser.AppImage").Apply(autostart, autostartExists: false);

        await Assert.That(autostart.IsEnabled).IsFalse();
    }

    [Test]
    public async Task Apply_MovedAppImage_FollowsIt()
    {
        using var folder = new TempFolder();
        folder
            .Integration("/home/me/Downloads/Micser.AppImage")
            .Apply(folder.Autostart("/home/me/Downloads/Micser.AppImage"), autostartExists: false);
        var moved = folder.Autostart("/home/me/Apps/Micser.AppImage");
        var integration = folder.Integration("/home/me/Apps/Micser.AppImage");

        integration.Apply(moved, autostartExists: true);

        await Assert.That(moved.IsEnabled).IsTrue();
        await Assert
            .That(await File.ReadAllLinesAsync(integration.DesktopEntryPath))
            .Contains("Exec=\"/home/me/Apps/Micser.AppImage\"");
    }

    private sealed class TempFolder : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        public XdgAutostart Autostart(string appImagePath)
        {
            return new XdgAutostart(appImagePath, Path.Combine(_path, "config", "autostart", "micser.desktop"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, recursive: true);
            }
        }

        public AppImageIntegration Integration(string appImagePath)
        {
            return new AppImageIntegration(
                appImagePath,
                Path.Combine(_path, "data"),
                Path.Combine(_path, "data", "Micser", "appimage-installed")
            );
        }
    }
}
