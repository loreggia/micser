using Microsoft.Win32;

namespace Micser.Shell.Tests;

public class AutostartTests
{
    [Test]
    public async Task SetEnabled_AddsAndRemovesTheEntry()
    {
        var keyPath = $@"Software\Micser.Tests\{Guid.NewGuid():N}";
        try
        {
            var autostart = new Autostart(@"C:\Program Files\Micser\Micser.Shell.exe", Registry.CurrentUser, keyPath);

            var initially = autostart.IsEnabled;
            autostart.SetEnabled(true);
            var enabled = autostart.IsEnabled;
            using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                await Assert.That(key!.GetValue("Micser")).IsEqualTo(@"""C:\Program Files\Micser\Micser.Shell.exe"" --minimized");
            }

            autostart.SetEnabled(false);

            await Assert.That(initially).IsFalse();
            await Assert.That(enabled).IsTrue();
            await Assert.That(autostart.IsEnabled).IsFalse();
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Micser.Tests", throwOnMissingSubKey: false);
        }
    }

    [Test]
    public async Task IsEnabled_EntryOfAnotherInstallation_IsFalse()
    {
        var keyPath = $@"Software\Micser.Tests\{Guid.NewGuid():N}";
        try
        {
            new Autostart(@"C:\Old\Micser.Shell.exe", Registry.CurrentUser, keyPath).SetEnabled(true);

            await Assert.That(new Autostart(@"C:\New\Micser.Shell.exe", Registry.CurrentUser, keyPath).IsEnabled).IsFalse();
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Micser.Tests", throwOnMissingSubKey: false);
        }
    }
}
