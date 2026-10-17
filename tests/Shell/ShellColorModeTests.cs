using Avalonia.Styling;
using Micser.Tests.Common;

namespace Micser.Shell.Tests;

public class ShellColorModeTests
{
    [Test]
    [Arguments(null, "Default")]
    [Arguments("Light", "Light")]
    [Arguments("Dark", "Dark")]
    [Arguments("Sepia", "Default")]
    public async Task Resolve_MapsThePreferenceToAThemeVariant(string? preference, string expected)
    {
        await Assert.That(ShellColorMode.Resolve(preference).Key).IsEqualTo(expected);
    }

    [Test]
    public async Task SetPreference_ChangesTheColorModeAndPersists()
    {
        using var directory = new TempDirectory();
        var path = directory.Combine("color-mode.json");
        var colorMode = new ShellColorMode(path);
        var changes = 0;
        colorMode.Changed += (_, _) => changes++;

        await Assert.That(colorMode.ThemeVariant).IsEqualTo(ThemeVariant.Default);

        colorMode.SetPreference("Dark");

        await Assert.That(changes).IsEqualTo(1);
        await Assert.That(colorMode.ThemeVariant).IsEqualTo(ThemeVariant.Dark);
        await Assert.That(new ShellColorMode(path).Preference).IsEqualTo("Dark");
    }

    [Test]
    public async Task SetPreference_SameColorMode_DoesNotRaiseChanged()
    {
        using var directory = new TempDirectory();
        var path = directory.Combine("color-mode.json");
        var colorMode = new ShellColorMode(path);
        var changes = 0;
        colorMode.Changed += (_, _) => changes++;

        colorMode.SetPreference("Sepia");

        await Assert.That(changes).IsEqualTo(0);
        await Assert.That(new ShellColorMode(path).Preference).IsEqualTo("Sepia");
    }
}
