namespace Micser.Engine;

/// <summary>
/// The "Engine" configuration section.
/// </summary>
public sealed class EngineOptions
{
    public const string SectionName = "Engine";

    /// <summary>
    /// The routing graph and settings.
    /// </summary>
    public string ConfigPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Micser", "config.json");

    /// <summary>
    /// Where the engine publishes its URL and access token for the shell.
    /// </summary>
    public string DiscoveryPath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Micser",
            "engine.json"
        );

    /// <summary>
    /// Loads the plugins' subgraph templates as built-in templates.
    /// </summary>
    public bool LoadPluginTemplates { get; set; } = true;

    /// <summary>
    /// The plugins shipped with the engine. A relative path is relative to the engine's folder.
    /// </summary>
    public string BuiltInPluginsPath { get; set; } = "plugins";

    /// <summary>
    /// The plugins the user installed. They're kept across updates.
    /// </summary>
    public string PluginsPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Micser", "plugins");

    /// <summary>
    /// Requires the access token on /api and /hubs requests.
    /// </summary>
    public bool RequireToken { get; set; } = true;

    /// <summary>
    /// Allows only one engine per user session.
    /// </summary>
    public bool SingleInstance { get; set; } = true;
}
