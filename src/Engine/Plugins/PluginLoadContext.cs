using System.Reflection;
using System.Runtime.Loader;

namespace Micser.Engine.Plugins;

/// <summary>
/// Loads a plugin's assemblies from its folder. Assemblies the engine has (Micser.Audio, NAudio, Microsoft.Extensions.*, the framework) come from
/// the default context, so the plugin and the engine share their types.
/// </summary>
internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private static readonly Lazy<HashSet<string>> HostAssemblies = new(ReadHostAssemblies);

    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string name, string assemblyPath)
        : base(name)
    {
        _resolver = new AssemblyDependencyResolver(assemblyPath);
    }

    public static bool IsHostAssembly(AssemblyName name)
    {
        return name.Name != null && HostAssemblies.Value.Contains(name.Name);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (IsHostAssembly(assemblyName))
        {
            return null;
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }

    private static HashSet<string> ReadHostAssemblies()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string paths)
        {
            foreach (var path in paths.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                names.Add(Path.GetFileNameWithoutExtension(path));
            }
        }

        foreach (var assembly in Default.Assemblies)
        {
            if (assembly.GetName().Name is { } name)
            {
                names.Add(name);
            }
        }

        return names;
    }
}
