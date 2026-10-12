using System.Text.Json;
using Avalonia.Styling;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// The color mode of the shell's windows and of the web view. It follows the color mode of the UI's theme preference, which the UI passes
/// on and which is kept here for the next start, so the window has it before the UI loads. Without a preference, or with one the shell
/// doesn't know, it follows the system.
/// </summary>
internal sealed class ShellColorMode
{
    private readonly string _path;

    public ShellColorMode(string path)
    {
        _path = path;
        Preference = Load(path);
    }

    public event EventHandler? Changed;

    public static string DefaultPath { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Micser",
            "color-mode.json"
        );

    /// <summary>
    /// The color mode of the UI's theme preference, "Light" or "Dark"; null follows the system.
    /// </summary>
    public string? Preference { get; private set; }

    /// <summary>
    /// The theme variant of the shell's windows; <see cref="ThemeVariant.Default"/> follows the system.
    /// </summary>
    public ThemeVariant ThemeVariant => Resolve(Preference);

    public static ThemeVariant Resolve(string? preference)
    {
        return preference switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }

    /// <summary>
    /// Changes the preference, saves it and raises <see cref="Changed"/> if the color mode changed.
    /// </summary>
    public void SetPreference(string? preference)
    {
        if (preference == Preference)
        {
            return;
        }

        var previous = ThemeVariant;
        Preference = preference;
        Save();
        if (ThemeVariant != previous)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private static string? Load(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<Stored>(File.ReadAllText(path), JsonSerializerOptions.Web)?.ColorMode;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
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
            Log.Warning(ex, "Saving the color mode failed.");
        }
    }

    private sealed record Stored(string? ColorMode);
}
