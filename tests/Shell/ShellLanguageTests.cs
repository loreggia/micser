using System.Globalization;

namespace Micser.Shell.Tests;

[NotInParallel(nameof(Strings))]
public class ShellLanguageTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    [Test]
    [Arguments("de", "en-US", "de")]
    [Arguments("en", "de-DE", "en")]
    [Arguments(null, "de-AT", "de")]
    [Arguments("fr", "de-DE", "de")]
    [Arguments(null, "fr-FR", "en")]
    public async Task Resolve_PrefersThePreferenceThenWindows(string? preference, string system, string expected)
    {
        await Assert.That(ShellLanguage.Resolve(preference, CultureInfo.GetCultureInfo(system))).IsEqualTo(expected);
    }

    [Test]
    public async Task SetPreference_TranslatesAndPersists()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "language.json");
        try
        {
            var language = new ShellLanguage(path, English);
            var changes = 0;
            language.Changed += (_, _) => changes++;

            await Assert.That(Strings.TrayExit).IsEqualTo("Exit Micser");

            language.SetPreference("de");

            await Assert.That(changes).IsEqualTo(1);
            await Assert.That(Strings.TrayExit).IsEqualTo("Micser beenden");
            await Assert.That(new ShellLanguage(path, English).Preference).IsEqualTo("de");
        }
        finally
        {
            Strings.Culture = null;
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Test]
    public async Task SetPreference_SameLanguage_DoesNotRaiseChanged()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "language.json");
        try
        {
            var language = new ShellLanguage(path, German);
            var changes = 0;
            language.Changed += (_, _) => changes++;

            language.SetPreference("de");

            await Assert.That(changes).IsEqualTo(0);
            await Assert.That(language.Culture.Name).IsEqualTo("de");
        }
        finally
        {
            Strings.Culture = null;
            if (Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
            }
        }
    }
}
