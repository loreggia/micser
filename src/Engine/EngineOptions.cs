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
    public string ConfigPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Micser", "config.json");

    /// <summary>
    /// Where the engine publishes its URL and access token for the shell.
    /// </summary>
    public string DiscoveryPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Micser", "engine.json");

    /// <summary>
    /// Requires the access token on /api and /hubs requests.
    /// </summary>
    public bool RequireToken { get; set; } = true;

    /// <summary>
    /// Allows only one engine per user session.
    /// </summary>
    public bool SingleInstance { get; set; } = true;
}
