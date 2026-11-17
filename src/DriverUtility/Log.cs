using System.Security.AccessControl;
using System.Security.Principal;

namespace Micser.DriverUtility;

/// <summary>
/// Logs to stderr (stdout carries the status JSON) and to %ProgramData%\Micser\logs\driver-utility.log: the driver is machine-wide, and a
/// folder in %LocalAppData%\Micser would make Velopack's setup take it for an existing installation.
/// </summary>
internal static class Log
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Micser",
        "logs",
        "driver-utility.log"
    );

    /// <summary>
    /// Lets all users write the log: the elevated commands create it, and the cable commands run without elevation.
    /// </summary>
    public static void AllowUsers()
    {
        try
        {
            var directory = Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var rule = new FileSystemAccessRule(
                users,
                FileSystemRights.Modify,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow
            );
            var security = directory.GetAccessControl();
            security.AddAccessRule(rule);
            directory.SetAccessControl(security);

            // a log created before keeps its own ACL
            if (File.Exists(FilePath))
            {
                var file = new FileInfo(FilePath);
                var fileSecurity = file.GetAccessControl();
                fileSecurity.AddAccessRule(
                    new FileSystemAccessRule(users, FileSystemRights.Modify, AccessControlType.Allow)
                );
                file.SetAccessControl(fileSecurity);
            }
        }
        catch (SystemException ex)
        {
            Warning($"Letting all users write the log failed: {ex.Message}");
        }
    }

    public static void Error(string message)
    {
        Write("ERR", message);
    }

    public static void Info(string message)
    {
        Write("INF", message);
    }

    public static void Warning(string message)
    {
        Write("WRN", message);
    }

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        Console.Error.WriteLine(line);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.AppendAllText(FilePath, line + Environment.NewLine);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
