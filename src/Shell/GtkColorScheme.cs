using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.X11.Interop;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// The light or dark mode of the web view on Linux. WebKitGTK's <c>prefers-color-scheme</c> doesn't follow the desktop's dark mode (the
/// portal's <c>color-scheme</c>), only GTK's "prefer dark theme" setting, which this sets for the process. GTK runs on Avalonia's GLib
/// thread, which the web view uses as well.
/// </summary>
[SupportedOSPlatform("linux")]
internal static partial class GtkColorScheme
{
    private const nint GTypeBoolean = 5 << 2;

    public static async Task ApplyAsync(bool isDark)
    {
        try
        {
            await GtkInteropHelper.RunOnGlibThread(() =>
            {
                var settings = gtk_settings_get_default();
                if (settings != 0)
                {
                    var value = default(GValue);
                    g_value_init(ref value, GTypeBoolean);
                    g_value_set_boolean(ref value, isDark ? 1 : 0);
                    g_object_set_property(settings, "gtk-application-prefer-dark-theme", ref value);
                    g_value_unset(ref value);
                }

                return true;
            });
        }
        catch (Exception ex)
            when (ex is InvalidOperationException or DllNotFoundException or EntryPointNotFoundException)
        {
            Log.Warning(ex, "Could not set the web view's color scheme.");
        }
    }

    [LibraryImport("libgtk-3.so.0")]
    private static partial nint gtk_settings_get_default();

    [LibraryImport("libgobject-2.0.so.0", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void g_object_set_property(nint target, string name, ref GValue value);

    [LibraryImport("libgobject-2.0.so.0")]
    private static partial nint g_value_init(ref GValue value, nint type);

    [LibraryImport("libgobject-2.0.so.0")]
    private static partial void g_value_set_boolean(ref GValue value, int boolean);

    [LibraryImport("libgobject-2.0.so.0")]
    private static partial void g_value_unset(ref GValue value);

    [StructLayout(LayoutKind.Sequential)]
    private struct GValue
    {
        public nint Type;
        public long Data0;
        public long Data1;
    }
}
