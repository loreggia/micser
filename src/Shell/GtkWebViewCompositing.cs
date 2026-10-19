using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.X11.Interop;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// Turns off WebKitGTK's hardware acceleration (accelerated compositing) on Linux. Avalonia's web view copies WebKit's window into an
/// Avalonia bitmap anyway, and composited layers come out blurry. A scrollable element (e.g. a module whose content overflows) gets a
/// composited layer, and so does the graph's React Flow viewport above it, whose fractional translation makes WebKit resample it and
/// lose subpixel antialiasing. GTK runs on Avalonia's GLib thread, which the web view uses as well.
/// </summary>
[SupportedOSPlatform("linux")]
internal static class GtkWebViewCompositing
{
    private const int HardwareAccelerationPolicyNever = 2;

    // the libraries Avalonia's web view loads, in its order; loading one that is loaded already returns it
    private static readonly string[] WebKitLibraries = ["libwebkit2gtk-4.1.so.0", "libwebkit2gtk-4.0.so.37"];

    public static async Task DisableAsync(nint webView)
    {
        try
        {
            await GtkInteropHelper.RunOnGlibThread(() => SetPolicyNever(webView));
        }
        catch (Exception ex)
            when (ex is InvalidOperationException or DllNotFoundException or EntryPointNotFoundException)
        {
            Log.Warning(ex, "Could not turn off the web view's hardware acceleration.");
        }
    }

    private static unsafe bool SetPolicyNever(nint webView)
    {
        var library = WebKitLibraries
            .Select(name => NativeLibrary.TryLoad(name, out var handle) ? handle : 0)
            .FirstOrDefault(handle => handle != 0);
        if (library == 0)
        {
            throw new DllNotFoundException("WebKitGTK isn't loaded.");
        }

        var getSettings = (delegate* unmanaged<nint, nint>)
            NativeLibrary.GetExport(library, "webkit_web_view_get_settings");
        var setPolicy = (delegate* unmanaged<nint, int, void>)
            NativeLibrary.GetExport(library, "webkit_settings_set_hardware_acceleration_policy");
        setPolicy(getSettings(webView), HardwareAccelerationPolicyNever);
        return true;
    }
}
