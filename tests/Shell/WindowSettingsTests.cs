using System.Drawing;
using Micser.Tests.Common;

namespace Micser.Shell.Tests;

public class WindowSettingsTests
{
    private static readonly Rectangle[] Screens = [new Rectangle(0, 0, 1920, 1040)];

    [Test]
    public async Task IsVisibleOn_WindowOnScreen_IsTrue()
    {
        await Assert.That(new WindowSettings(100, 100, 800, 600, false).IsVisibleOn(Screens)).IsTrue();
    }

    [Test]
    [Arguments(3000, 100)]
    [Arguments(100, -500)]
    [Arguments(1880, 100)]
    public async Task IsVisibleOn_TitleBarOffScreen_IsFalse(int x, int y)
    {
        await Assert.That(new WindowSettings(x, y, 800, 600, false).IsVisibleOn(Screens)).IsFalse();
    }

    [Test]
    public async Task SaveAndLoad_RoundTrips()
    {
        using var directory = new TempDirectory();
        var path = directory.Combine("shell.json");
        var settings = new WindowSettings(10, 20, 1280, 800, true);

        settings.Save(path);

        await Assert.That(WindowSettings.Load(path)).IsEqualTo(settings);
    }

    [Test]
    public async Task Load_MissingFile_ReturnsNull()
    {
        using var directory = new TempDirectory();

        await Assert.That(WindowSettings.Load(directory.Combine("shell.json"))).IsNull();
    }
}
