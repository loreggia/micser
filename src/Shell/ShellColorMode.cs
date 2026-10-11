using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// The color mode of the shell's windows and of the web view. It follows the color mode of the UI's theme preference, which the UI passes
/// on and which is kept here for the next start, so the window and the tray menu have it before the UI loads (WinForms applies a color mode
/// fully only to what is created after it's set). Without a preference, or with one the shell doesn't know, it follows Windows.
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

    public SystemColorMode ColorMode => Resolve(Preference);

    public static string DefaultPath { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Micser",
            "color-mode.json"
        );

    /// <summary>
    /// The color scheme the web view reports to pages (<c>prefers-color-scheme</c>).
    /// </summary>
    public CoreWebView2PreferredColorScheme PreferredColorScheme =>
        ColorMode switch
        {
            SystemColorMode.Classic => CoreWebView2PreferredColorScheme.Light,
            SystemColorMode.Dark => CoreWebView2PreferredColorScheme.Dark,
            _ => CoreWebView2PreferredColorScheme.Auto,
        };

    /// <summary>
    /// The color mode of the UI's theme preference, "Light" or "Dark"; null follows Windows.
    /// </summary>
    public string? Preference { get; private set; }

    public static SystemColorMode Resolve(string? preference)
    {
        return preference switch
        {
            "Light" => SystemColorMode.Classic,
            "Dark" => SystemColorMode.Dark,
            _ => SystemColorMode.System,
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

        var previous = ColorMode;
        Preference = preference;
        Save();
        if (ColorMode != previous)
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
