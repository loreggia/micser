using System.IO.Compression;

namespace Micser.Engine.Plugins;

/// <summary>
/// A plugin change that's applied at the next engine start.
/// </summary>
public enum PluginChange
{
    None,
    Install,
    Remove,
}

/// <summary>
/// Installs and removes user plugins. A loaded plugin's files are in use, so changes are staged in <c>.pending</c> in the user plugin folder and
/// applied by <see cref="PluginLoader"/> at the next start.
/// </summary>
public sealed class PluginInstaller
{
    public const long MaxPackageBytes = 256L * 1024 * 1024;

    private const string PendingFolder = ".pending";
    private const string RemoveSuffix = ".remove";

    private readonly PluginCatalog _catalog;
    private readonly Lock _lock = new();
    private readonly string _pendingPath;
    private readonly string _userPath;

    public PluginInstaller(string userPath, PluginCatalog catalog)
    {
        _userPath = userPath;
        _pendingPath = Path.Combine(userPath, PendingFolder);
        _catalog = catalog;
    }

    /// <summary>
    /// Moves staged installs into place and deletes plugins staged for removal. Returns the errors.
    /// </summary>
    public static List<string> ApplyPending(string userPath)
    {
        var errors = new List<string>();
        var pending = Path.Combine(userPath, PendingFolder);
        if (!Directory.Exists(pending))
        {
            return errors;
        }

        foreach (var marker in Directory.EnumerateFiles(pending, "*" + RemoveSuffix))
        {
            var id = Path.GetFileName(marker)[..^RemoveSuffix.Length];
            try
            {
                var target = Path.Combine(userPath, id);
                if (PluginManifest.IsValidId(id) && Directory.Exists(target))
                {
                    Directory.Delete(target, recursive: true);
                }

                File.Delete(marker);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add($"Removing {id}: {ex.Message}");
            }
        }

        foreach (var staged in Directory.EnumerateDirectories(pending))
        {
            var id = Path.GetFileName(staged);
            try
            {
                if (!PluginManifest.IsValidId(id))
                {
                    Directory.Delete(staged, recursive: true);
                    continue;
                }

                var target = Path.Combine(userPath, id);
                if (Directory.Exists(target))
                {
                    Directory.Delete(target, recursive: true);
                }

                Directory.Move(staged, target);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add($"Installing {id}: {ex.Message}");
            }
        }

        return errors;
    }

    /// <summary>
    /// The change staged for a plugin.
    /// </summary>
    public PluginChange GetPendingChange(string id)
    {
        lock (_lock)
        {
            return Directory.Exists(Path.Combine(_pendingPath, id)) ? PluginChange.Install
                : File.Exists(Path.Combine(_pendingPath, id + RemoveSuffix)) ? PluginChange.Remove
                : PluginChange.None;
        }
    }

    /// <summary>
    /// The plugins staged for installation.
    /// </summary>
    public IReadOnlyList<PluginManifest> GetPendingInstalls()
    {
        lock (_lock)
        {
            if (!Directory.Exists(_pendingPath))
            {
                return [];
            }

            var manifests = new List<PluginManifest>();
            foreach (
                var directory in Directory
                    .EnumerateDirectories(_pendingPath)
                    .Where(d => PluginManifest.IsValidId(Path.GetFileName(d)))
            )
            {
                try
                {
                    manifests.Add(PluginManifest.Read(directory));
                }
                catch (InvalidDataException)
                {
                    // not a complete package; ApplyPending moves it anyway and loading reports it
                }
            }

            return manifests;
        }
    }

    /// <summary>
    /// Stages a plugin package (a zip with <c>plugin.json</c> at its root) for installation, replacing an installed or staged plugin with the
    /// same id.
    /// </summary>
    /// <exception cref="InvalidDataException">The package is invalid or its id belongs to a built-in plugin.</exception>
    public PluginManifest StageInstall(Stream package)
    {
        lock (_lock)
        {
            Directory.CreateDirectory(_pendingPath);
            var extracted = Path.Combine(_pendingPath, ".tmp-" + Guid.NewGuid().ToString("N"));
            try
            {
                Extract(package, extracted);
                var manifest = PluginManifest.Read(extracted);
                if (IsBuiltIn(manifest.Id))
                {
                    throw new InvalidDataException($"'{manifest.Id}' is a built-in plugin.");
                }

                var target = Path.Combine(_pendingPath, manifest.Id);
                if (Directory.Exists(target))
                {
                    Directory.Delete(target, recursive: true);
                }

                File.Delete(Path.Combine(_pendingPath, manifest.Id + RemoveSuffix));
                Directory.Move(extracted, target);
                return manifest;
            }
            finally
            {
                if (Directory.Exists(extracted))
                {
                    Directory.Delete(extracted, recursive: true);
                }
            }
        }
    }

    /// <summary>
    /// Stages a user plugin for removal, or cancels its staged installation if it isn't installed.
    /// </summary>
    /// <returns>False if there is no such user plugin.</returns>
    /// <exception cref="InvalidDataException">The plugin is built in.</exception>
    public bool StageRemoval(string id)
    {
        lock (_lock)
        {
            if (!PluginManifest.IsValidId(id))
            {
                return false;
            }

            if (IsBuiltIn(id))
            {
                throw new InvalidDataException($"'{id}' is a built-in plugin and can't be removed.");
            }

            var staged = Path.Combine(_pendingPath, id);
            var wasStaged = Directory.Exists(staged);
            if (wasStaged)
            {
                Directory.Delete(staged, recursive: true);
            }

            if (!Directory.Exists(Path.Combine(_userPath, id)))
            {
                return wasStaged;
            }

            Directory.CreateDirectory(_pendingPath);
            File.WriteAllText(Path.Combine(_pendingPath, id + RemoveSuffix), "");
            return true;
        }
    }

    private static void Extract(Stream package, string directory)
    {
        ZipArchive archive;
        try
        {
            archive = new ZipArchive(package, ZipArchiveMode.Read);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException($"The package isn't a zip file: {ex.Message}", ex);
        }

        using (archive)
        {
            if (archive.Entries.Sum(e => e.Length) > MaxPackageBytes)
            {
                throw new InvalidDataException("The package is too large.");
            }

            try
            {
                // throws for entries outside the directory
                archive.ExtractToDirectory(directory);
            }
            catch (IOException ex)
            {
                throw new InvalidDataException($"The package can't be extracted: {ex.Message}", ex);
            }
        }
    }

    private bool IsBuiltIn(string id)
    {
        return _catalog.Plugins.Any(p => p.IsBuiltIn && string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
