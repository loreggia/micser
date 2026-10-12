namespace Micser.Shell.Tests;

public class XdgAutostartTests
{
    [Test]
    public async Task SetEnabled_WritesAndDeletesTheDesktopEntry()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "autostart", "micser.desktop");
        try
        {
            var autostart = new XdgAutostart("/opt/micser/Micser.Shell", path);

            var initially = autostart.IsEnabled;
            autostart.SetEnabled(true);
            var enabled = autostart.IsEnabled;
            var lines = File.ReadAllLines(path);
            autostart.SetEnabled(false);

            await Assert.That(initially).IsFalse();
            await Assert.That(enabled).IsTrue();
            await Assert.That(lines[0]).IsEqualTo("[Desktop Entry]");
            await Assert.That(lines).Contains("Type=Application");
            await Assert.That(lines).Contains("Exec=\"/opt/micser/Micser.Shell\" --minimized");
            await Assert.That(autostart.IsEnabled).IsFalse();
            await Assert.That(File.Exists(path)).IsFalse();
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    public async Task SetEnabled_FalseWithoutTheFolder_DoesNothing()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "autostart", "micser.desktop");

        new XdgAutostart("/opt/micser/Micser.Shell", path).SetEnabled(false);

        await Assert.That(File.Exists(path)).IsFalse();
    }

    [Test]
    public async Task IsEnabled_EntryOfAnotherInstallation_IsFalse()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "micser.desktop");
        try
        {
            new XdgAutostart("/old/Micser.Shell", path).SetEnabled(true);

            await Assert.That(new XdgAutostart("/new/Micser.Shell", path).IsEnabled).IsFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    [Arguments("/opt/micser/Micser.Shell", "\"/opt/micser/Micser.Shell\"")]
    [Arguments("/home/a b/Micser.Shell", "\"/home/a b/Micser.Shell\"")]
    [Arguments("/x/$HOME/`a`\"b\"", "\"/x/\\\\$HOME/\\\\`a\\\\`\\\\\"b\\\\\"\"")]
    [Arguments("/x\\y", "\"/x\\\\\\\\y\"")]
    public async Task Quote_EscapesForTheExecKey(string argument, string expected)
    {
        await Assert.That(XdgAutostart.Quote(argument)).IsEqualTo(expected);
    }
}
