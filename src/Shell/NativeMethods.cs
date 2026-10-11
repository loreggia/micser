using System.Runtime.InteropServices;

namespace Micser.Shell;

internal static partial class NativeMethods
{
    public const uint DwmwaUseImmersiveDarkMode = 20;

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmSetWindowAttribute(nint hwnd, uint attribute, in int value, int size);
}
