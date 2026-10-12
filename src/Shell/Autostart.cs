using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Micser.Shell;

/// <summary>
/// Starts the shell minimized when the user logs in.
/// </summary>
internal interface IAutostart
{
    /// <summary>
    /// Whether the entry exists and starts this executable.
    /// </summary>
    bool IsEnabled { get; }

    void SetEnabled(bool enabled);
}

internal static class Autostart
{
    /// <summary>
    /// The current platform's autostart entry for the executable.
    /// </summary>
    public static IAutostart Create(string executablePath)
    {
        return OperatingSystem.IsWindows()
            ? new WindowsAutostart(executablePath)
            : new XdgAutostart(executablePath, XdgAutostart.DefaultPath);
    }
}

/// <summary>
/// The "start with Windows" entry: a value in the user's Run key that starts the shell minimized at login.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsAutostart : IAutostart
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Micser";

    private readonly string _command;
    private readonly string _keyPath;
    private readonly RegistryKey _root;

    public WindowsAutostart(string executablePath, RegistryKey? root = null, string keyPath = RunKeyPath)
    {
        _command = $"\"{executablePath}\" {ShellOptions.MinimizedArgument}";
        _root = root ?? Registry.CurrentUser;
        _keyPath = keyPath;
    }

    public bool IsEnabled
    {
        get
        {
            using var key = _root.OpenSubKey(_keyPath);
            return string.Equals(key?.GetValue(ValueName) as string, _command, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = _root.CreateSubKey(_keyPath, writable: true);
        if (enabled)
        {
            key.SetValue(ValueName, _command, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}

/// <summary>
/// The "start at login" entry of freedesktop.org desktops (GNOME, KDE, Cinnamon, ...): a desktop entry in the user's autostart folder
/// (<c>$XDG_CONFIG_HOME/autostart</c>) that starts the shell minimized.
/// </summary>
internal sealed class XdgAutostart : IAutostart
{
    private readonly string _exec;
    private readonly string _path;

    public XdgAutostart(string executablePath, string path)
    {
        _exec = $"{Quote(executablePath)} {ShellOptions.MinimizedArgument}";
        _path = path;
    }

    public static string DefaultPath =>
        Path.Combine(
            Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } config
                ? config
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config"),
            "autostart",
            "micser.desktop"
        );

    public bool IsEnabled
    {
        get
        {
            try
            {
                return File.ReadLines(_path).Any(line => line == "Exec=" + _exec);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(
            _path,
            $"""
            [Desktop Entry]
            Type=Application
            Name=Micser
            Comment=Audio router
            Exec={_exec}
            Icon=micser
            Terminal=false
            X-GNOME-Autostart-enabled=true

            """
        );
    }

    /// <summary>
    /// An argument in a desktop entry's <c>Exec</c> key: quoted, with <c>"</c>, <c>`</c>, <c>$</c> and <c>\</c> escaped. The escaping
    /// backslashes are doubled, since the file's string escapes apply before the quoting (<c>\\$</c> for <c>$</c>).
    /// </summary>
    internal static string Quote(string argument)
    {
        var escaped = argument
            .Replace("\\", "\\\\\\\\")
            .Replace("\"", "\\\\\"")
            .Replace("`", "\\\\`")
            .Replace("$", "\\\\$");
        return $"\"{escaped}\"";
    }
}
