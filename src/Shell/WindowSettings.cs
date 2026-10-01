using System.Text.Json;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// The main window's last position and size.
/// </summary>
internal sealed record WindowSettings(int X, int Y, int Width, int Height, bool IsMaximized)
{
    public static string DefaultPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Micser", "shell.json");

    /// <summary>
    /// Returns the saved settings, or null if there are none or they're unreadable.
    /// </summary>
    public static WindowSettings? Load(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<WindowSettings>(File.ReadAllText(path), JsonSerializerOptions.Web);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Whether enough of the window would be on a screen to grab it, e.g. after a monitor was disconnected.
    /// </summary>
    public bool IsVisibleOn(IEnumerable<Rectangle> screens)
    {
        var titleBar = new Rectangle(X, Y, Width, 40);
        return Width > 0 && Height > 0 && screens.Any(screen =>
        {
            var visible = Rectangle.Intersect(screen, titleBar);
            return visible.Width >= 100 && visible.Height >= 20;
        });
    }

    public void Save(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonSerializerOptions.Web));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "Saving the window settings failed.");
        }
    }
}
