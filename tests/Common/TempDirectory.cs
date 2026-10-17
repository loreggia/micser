namespace Micser.Tests.Common;

/// <summary>
/// A unique directory below the temp folder (<c>micser-tests/&lt;guid&gt;</c>), deleted with its contents on dispose. The constructor only
/// picks the path, e.g. for code that creates the directory itself; <see cref="Create"/> creates it too.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "micser-tests", Guid.NewGuid().ToString("N"));
    }

    public string Path { get; }

    public static TempDirectory Create()
    {
        var directory = new TempDirectory();
        Directory.CreateDirectory(directory.Path);
        return directory;
    }

    /// <summary>
    /// Combines <see cref="Path"/> with <paramref name="paths"/>.
    /// </summary>
    public string Combine(params string[] paths)
    {
        return System.IO.Path.Combine([Path, .. paths]);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // e.g. loaded plugin assemblies, which stay locked until the process exits
        }
    }
}
