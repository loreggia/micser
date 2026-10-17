using System.Globalization;
using Micser.Tests.Common;

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
        using var directory = new TempDirectory();
        var path = directory.Combine("language.json");
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
        }
    }

    [Test]
    public async Task SetPreference_SameLanguage_DoesNotRaiseChanged()
    {
        using var directory = new TempDirectory();
        var path = directory.Combine("language.json");
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
        }
    }
}
