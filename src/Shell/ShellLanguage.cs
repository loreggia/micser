using System.Globalization;
using System.Text.Json;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// The language of the shell's texts (<see cref="Strings"/>). It follows the UI's language preference, which the UI passes on and which is
/// kept here for the next start, so the tray menu has it before the UI loads. Without a preference, or with a language the shell doesn't
/// have, it follows the Windows display language, falling back to English.
/// </summary>
internal sealed class ShellLanguage
{
    private static readonly string[] SupportedLanguages = ["en", "de"];

    private readonly string _path;
    private readonly CultureInfo _systemCulture;

    /// <param name="systemCulture">The Windows display language; default: the current UI culture.</param>
    public ShellLanguage(string path, CultureInfo? systemCulture = null)
    {
        _path = path;
        _systemCulture = systemCulture ?? CultureInfo.CurrentUICulture;
        Preference = Load(path);
        Apply();
    }

    public event EventHandler? Changed;

    /// <summary>
    /// The culture of the shell's texts.
    /// </summary>
    public CultureInfo Culture { get; private set; } = CultureInfo.InvariantCulture;

    public static string DefaultPath { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Micser",
            "language.json"
        );

    /// <summary>
    /// The UI's language preference, e.g. "de"; null follows Windows.
    /// </summary>
    public string? Preference { get; private set; }

    /// <summary>
    /// The shell's language for a preference: the preference or the Windows display language if the shell has it, otherwise English.
    /// </summary>
    public static string Resolve(string? preference, CultureInfo systemCulture)
    {
        if (preference != null && SupportedLanguages.Contains(preference))
        {
            return preference;
        }

        return SupportedLanguages.Contains(systemCulture.TwoLetterISOLanguageName)
            ? systemCulture.TwoLetterISOLanguageName
            : SupportedLanguages[0];
    }

    /// <summary>
    /// Changes the preference, saves it and raises <see cref="Changed"/> if the language changed.
    /// </summary>
    public void SetPreference(string? preference)
    {
        if (preference == Preference)
        {
            return;
        }

        Preference = preference;
        Save();
        var previous = Culture;
        Apply();
        if (!Equals(Culture, previous))
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private static string? Load(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<Stored>(File.ReadAllText(path), JsonSerializerOptions.Web)?.Language;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private void Apply()
    {
        Culture = CultureInfo.GetCultureInfo(Resolve(Preference, _systemCulture));
        Strings.Culture = Culture;
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(new Stored(Preference), JsonSerializerOptions.Web));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "Saving the language failed.");
        }
    }

    private sealed record Stored(string? Language);
}
