using Microsoft.Web.WebView2.Core;

namespace Micser.Shell.Tests;

public class ShellColorModeTests
{
    [Test]
    [Arguments(null, SystemColorMode.System)]
    [Arguments("Light", SystemColorMode.Classic)]
    [Arguments("Dark", SystemColorMode.Dark)]
    [Arguments("Sepia", SystemColorMode.System)]
    public async Task Resolve_MapsThePreferenceToAColorMode(string? preference, SystemColorMode expected)
    {
        await Assert.That(ShellColorMode.Resolve(preference)).IsEqualTo(expected);
    }

    [Test]
    public async Task SetPreference_ChangesTheColorModeAndPersists()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "color-mode.json");
        try
        {
            var colorMode = new ShellColorMode(path);
            var changes = 0;
            colorMode.Changed += (_, _) => changes++;

            await Assert.That(colorMode.PreferredColorScheme).IsEqualTo(CoreWebView2PreferredColorScheme.Auto);

            colorMode.SetPreference("Dark");

            await Assert.That(changes).IsEqualTo(1);
            await Assert.That(colorMode.PreferredColorScheme).IsEqualTo(CoreWebView2PreferredColorScheme.Dark);
            await Assert.That(new ShellColorMode(path).Preference).IsEqualTo("Dark");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Test]
    public async Task SetPreference_SameColorMode_DoesNotRaiseChanged()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "color-mode.json");
        try
        {
            var colorMode = new ShellColorMode(path);
            var changes = 0;
            colorMode.Changed += (_, _) => changes++;

            colorMode.SetPreference("Sepia");

            await Assert.That(changes).IsEqualTo(0);
            await Assert.That(new ShellColorMode(path).Preference).IsEqualTo("Sepia");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}
