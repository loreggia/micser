using Micser.Engine.Audio;
using Micser.Engine.Contracts;

namespace Micser.Engine.Plugins;

/// <summary>
/// Lists the plugins and stages installs and removals, which take effect when the engine restarts.
/// </summary>
public sealed class PluginService
{
    private readonly PluginCatalog _catalog;
    private readonly PluginInstaller _installer;
    private readonly IEngineNotifier _notifier;

    public PluginService(PluginCatalog catalog, PluginInstaller installer, IEngineNotifier notifier)
    {
        _catalog = catalog;
        _installer = installer;
        _notifier = notifier;
    }

    /// <summary>
    /// The URL path where a loaded plugin's widget bundle is served: <c>/plugins/{id}/{web}</c>.
    /// </summary>
    public static string GetWebUrl(PluginManifest manifest)
    {
        return $"/plugins/{manifest.Id}/{manifest.WebPath}";
    }

    public IReadOnlyList<PluginDto> GetPlugins()
    {
        var plugins = _catalog.Plugins
            .Select(p => new PluginDto(
                p.Id,
                p.Manifest?.Name,
                p.Manifest?.Version,
                p.IsBuiltIn,
                p.IsLoaded,
                p.Error,
                p.IsLoaded && p.Manifest!.WebPath != null ? GetWebUrl(p.Manifest) : null,
                p.IsBuiltIn ? PluginChange.None : _installer.GetPendingChange(p.Id)))
            .ToList();

        // staged plugins that aren't installed yet
        foreach (var manifest in _installer.GetPendingInstalls())
        {
            if (!plugins.Any(p => !p.IsBuiltIn && string.Equals(p.Id, manifest.Id, StringComparison.OrdinalIgnoreCase)))
            {
                plugins.Add(new PluginDto(manifest.Id, manifest.Name, manifest.Version, false, false, null, null, PluginChange.Install));
            }
        }

        return plugins;
    }

    /// <exception cref="EngineRequestException">The package is invalid.</exception>
    public PluginDto Install(Stream package)
    {
        PluginManifest manifest;
        try
        {
            manifest = _installer.StageInstall(package);
        }
        catch (InvalidDataException ex)
        {
            throw EngineRequestException.Invalid(ex.Message);
        }

        var plugins = GetPlugins();
        _notifier.PluginsChanged(plugins);
        return plugins.First(p => !p.IsBuiltIn && string.Equals(p.Id, manifest.Id, StringComparison.OrdinalIgnoreCase));
    }

    /// <returns>False if there is no such user plugin.</returns>
    /// <exception cref="EngineRequestException">The plugin is built in.</exception>
    public bool Remove(string id)
    {
        bool removed;
        try
        {
            removed = _installer.StageRemoval(id);
        }
        catch (InvalidDataException ex)
        {
            throw EngineRequestException.Invalid(ex.Message);
        }

        if (removed)
        {
            _notifier.PluginsChanged(GetPlugins());
        }

        return removed;
    }
}
