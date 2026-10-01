using Microsoft.Win32;

namespace Micser.Shell;

/// <summary>
/// The "start with Windows" entry: a value in the user's Run key that starts the shell minimized at login.
/// </summary>
internal sealed class Autostart
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Micser";

    private readonly string _command;
    private readonly string _keyPath;
    private readonly RegistryKey _root;

    public Autostart(string executablePath, RegistryKey? root = null, string keyPath = RunKeyPath)
    {
        _command = $"\"{executablePath}\" {ShellOptions.MinimizedArgument}";
        _root = root ?? Registry.CurrentUser;
        _keyPath = keyPath;
    }

    /// <summary>
    /// Whether the entry exists and starts this executable.
    /// </summary>
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
