namespace Micser.DriverUtility;

/// <summary>
/// Logs to stderr (stdout carries the status JSON) and to %ProgramData%\Micser\logs\driver-utility.log: the driver is machine-wide, and a
/// folder in %LocalAppData%\Micser would make Velopack's setup take it for an existing installation.
/// </summary>
internal static class Log
{
    private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Micser", "logs", "driver-utility.log");

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
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
