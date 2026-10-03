namespace Micser.Engine.Plugins;

/// <summary>
/// A plugin found at startup.
/// </summary>
/// <param name="Id">The manifest's id, or the folder name if the manifest can't be read.</param>
/// <param name="Directory">The plugin folder.</param>
/// <param name="Manifest">Null if the manifest can't be read.</param>
/// <param name="Error">Why the plugin isn't loaded; null if it is.</param>
public sealed record PluginInfo(string Id, string Directory, bool IsBuiltIn, PluginManifest? Manifest, string? Error, Exception? Exception = null)
{
    public bool IsLoaded => Error == null;
}

/// <summary>
/// The plugins found at startup, loaded or not. Plugins are loaded once per engine process.
/// </summary>
public sealed class PluginCatalog
{
    private readonly IReadOnlyList<string> _setupErrors;

    public PluginCatalog(IReadOnlyList<PluginInfo> plugins, IReadOnlyList<string> setupErrors)
    {
        Plugins = plugins;
        _setupErrors = setupErrors;
    }

    public IReadOnlyList<PluginInfo> Plugins { get; }

    /// <summary>
    /// Logs the result of loading; plugins are loaded before logging is available.
    /// </summary>
    public void LogResults(ILogger logger)
    {
        foreach (var error in _setupErrors)
        {
            logger.LogError("Applying a pending plugin change failed: {Error}", error);
        }

        foreach (var plugin in Plugins)
        {
            if (plugin.IsLoaded)
            {
                logger.LogInformation("Loaded plugin {Id} {Version} from {Directory}.", plugin.Id, plugin.Manifest!.Version, plugin.Directory);
            }
            else
            {
                logger.LogError(plugin.Exception, "The plugin {Id} in {Directory} isn't loaded: {Error}", plugin.Id, plugin.Directory, plugin.Error);
            }
        }
    }

    /// <summary>
    /// Returns a loaded plugin.
    /// </summary>
    public PluginInfo? TryGetLoaded(string id)
    {
        return Plugins.FirstOrDefault(p => p.IsLoaded && string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
