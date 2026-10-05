using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Micser.Engine.Plugins;

/// <summary>
/// A plugin's <c>plugin.json</c>.
/// </summary>
/// <param name="Id">Identifies the plugin and names its folder.</param>
/// <param name="Assembly">The file name of the assembly containing the <see cref="Audio.IAudioPlugin"/>.</param>
/// <param name="Web">The widget bundle's entry, relative to the plugin folder (e.g. <c>web/index.js</c>), if the plugin has widgets.</param>
/// <param name="Templates">
/// A JSON file with subgraph templates, relative to the plugin folder, that the engine adds to the configuration once each.
/// </param>
public sealed partial record PluginManifest(string Id, string Name, string Version, string Assembly, string? Web, string? Templates = null)
{
    public const string FileName = "plugin.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// <see cref="Web"/> with forward slashes and without a leading slash, e.g. <c>web/index.js</c>.
    /// </summary>
    [JsonIgnore]
    public string? WebPath => Web?.Replace('\\', '/').TrimStart('/');

    public static bool IsValidId(string? id)
    {
        return id != null && IdPattern().IsMatch(id) && id.Trim('.').Length > 0;
    }

    /// <summary>
    /// Reads and validates the manifest of the plugin in <paramref name="directory"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">The manifest is missing or invalid.</exception>
    public static PluginManifest Read(string directory)
    {
        var path = Path.Combine(directory, FileName);
        if (!File.Exists(path))
        {
            throw new InvalidDataException($"The plugin has no {FileName}.");
        }

        PluginManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(path), Json);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"The plugin's {FileName} can't be read: {ex.Message}", ex);
        }

        if (manifest == null || !IsValidId(manifest.Id))
        {
            throw new InvalidDataException("The plugin's id is missing or invalid (letters, digits, '.', '_' and '-').");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name) || string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidDataException("The plugin's name or version is missing.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Assembly) || Path.GetFileName(manifest.Assembly) != manifest.Assembly)
        {
            throw new InvalidDataException("The plugin's assembly must be a file name in the plugin folder.");
        }

        if (manifest.Web != null && !IsInside(directory, manifest.Web))
        {
            throw new InvalidDataException("The plugin's web entry must be inside the plugin folder.");
        }

        if (manifest.Templates != null && !IsInside(directory, manifest.Templates))
        {
            throw new InvalidDataException("The plugin's templates file must be inside the plugin folder.");
        }

        return manifest;
    }

    private static bool IsInside(string directory, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return false;
        }

        var root = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(Path.Combine(root, relativePath)).StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex IdPattern();
}
