using System.Reflection;
using Micser.Audio;

namespace Micser.Engine.Plugins;

/// <summary>
/// Loads the plugins from the built-in and the user plugin folder (one subfolder per plugin, named by its id) and registers their services.
/// </summary>
public static class PluginLoader
{
    /// <summary>
    /// Applies pending installs and removals in <paramref name="userPath"/>, then loads the built-in plugins and the user's. A plugin that fails to
    /// load registers nothing; a user plugin can't replace a built-in one.
    /// </summary>
    /// <param name="userPath">The user plugin folder; null loads only the built-in plugins.</param>
    public static PluginCatalog Load(IServiceCollection services, string builtInPath, string? userPath)
    {
        var setupErrors = userPath != null ? PluginInstaller.ApplyPending(userPath) : [];
        var plugins = new List<PluginInfo>();

        foreach (var (root, isBuiltIn) in new[] { (builtInPath, true), (userPath, false) })
        {
            if (root == null || !Directory.Exists(root))
            {
                continue;
            }

            var directories = Directory
                .EnumerateDirectories(root)
                .Where(d => !Path.GetFileName(d).StartsWith('.'))
                .Order(StringComparer.OrdinalIgnoreCase);

            foreach (var directory in directories)
            {
                plugins.Add(LoadPlugin(services, directory, isBuiltIn, plugins));
            }
        }

        return new PluginCatalog(plugins, setupErrors);
    }

    private static Type FindPluginType(Assembly assembly)
    {
        var types = assembly
            .GetExportedTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IAudioPlugin).IsAssignableFrom(t))
            .ToArray();

        return types.Length == 1
            ? types[0]
            : throw new InvalidDataException(
                $"The assembly {assembly.GetName().Name} must have exactly one public {nameof(IAudioPlugin)}, found {types.Length}."
            );
    }

    private static Assembly LoadAssembly(PluginManifest manifest, string assemblyPath)
    {
        var name = AssemblyName.GetAssemblyName(assemblyPath);

        // an assembly the engine already has (e.g. a test referencing the plugin) is shared instead of loaded twice
        return PluginLoadContext.IsHostAssembly(name)
            ? System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyName(name)
            : new PluginLoadContext(manifest.Id, assemblyPath).LoadFromAssemblyPath(assemblyPath);
    }

    private static PluginInfo LoadPlugin(
        IServiceCollection services,
        string directory,
        bool isBuiltIn,
        IReadOnlyList<PluginInfo> loaded
    )
    {
        var folderName = Path.GetFileName(directory);
        PluginManifest? manifest = null;
        try
        {
            manifest = PluginManifest.Read(directory);
            if (!string.Equals(manifest.Id, folderName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The plugin's folder must be named by its id '{manifest.Id}'.");
            }

            if (loaded.Any(p => p.IsLoaded && string.Equals(p.Id, manifest.Id, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidDataException($"A plugin with the id '{manifest.Id}' is already loaded.");
            }

            var assemblyPath = Path.Combine(directory, manifest.Assembly);
            if (!File.Exists(assemblyPath))
            {
                throw new InvalidDataException($"The plugin's assembly {manifest.Assembly} is missing.");
            }

            var plugin = (IAudioPlugin)Activator.CreateInstance(FindPluginType(LoadAssembly(manifest, assemblyPath)))!;

            // registered separately first, so a plugin that throws leaves nothing behind
            var pluginServices = new ServiceCollection();
            plugin.ConfigureServices(pluginServices);
            foreach (var descriptor in pluginServices)
            {
                services.Add(descriptor);
            }

            return new PluginInfo(manifest.Id, directory, isBuiltIn, manifest, null);
        }
        catch (Exception ex)
        {
            var error = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
            return new PluginInfo(
                manifest?.Id ?? folderName,
                directory,
                isBuiltIn,
                manifest,
                error.Message,
                error is InvalidDataException ? null : error
            );
        }
    }
}
