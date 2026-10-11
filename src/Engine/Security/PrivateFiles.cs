namespace Micser.Engine.Security;

/// <summary>
/// Creates folders and files that only the user can read. On Windows, the user's profile folders already are; on Unix, the modes are set
/// explicitly (0700 for folders, 0600 for files), since e.g. a home folder may be readable by other users.
/// </summary>
internal static class PrivateFiles
{
    private const UnixFileMode PrivateDirectoryMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode PrivateFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    /// <summary>
    /// Creates the folder if it doesn't exist; an existing folder keeps its mode.
    /// </summary>
    public static void CreateDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(path);
        }
        else
        {
            Directory.CreateDirectory(path, PrivateDirectoryMode);
        }
    }

    /// <summary>
    /// Creates a file the user can open exclusively, or opens the existing one; null if another process has it open.
    /// </summary>
    public static FileStream? TryOpenExclusive(string path)
    {
        try
        {
            return new FileStream(path, Options(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static void WriteAllText(string path, string contents)
    {
        using var stream = new FileStream(path, Options(FileMode.Create, FileAccess.Write, FileShare.Read));
        if (!OperatingSystem.IsWindows())
        {
            // the mode applies only to a new file, not to one left behind e.g. by a crash
            File.SetUnixFileMode(stream.SafeFileHandle, PrivateFileMode);
        }

        using var writer = new StreamWriter(stream);
        writer.Write(contents);
    }

    private static FileStreamOptions Options(FileMode mode, FileAccess access, FileShare share)
    {
        var options = new FileStreamOptions
        {
            Mode = mode,
            Access = access,
            Share = share,
        };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = PrivateFileMode;
        }

        return options;
    }
}
