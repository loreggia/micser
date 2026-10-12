using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Serilog;

namespace Micser.Shell;

/// <summary>
/// The window icon on X11 (<c>_NET_WM_ICON</c>), which the taskbar shows. Avalonia sets a 128 px image with premultiplied colors, which
/// the window manager reads as straight alpha (dark edges); the panel scales it down to about 32 px (hard edges). This sets a 64 px
/// image, scaled at high quality, with straight alpha. More sizes don't help: Cinnamon's window manager takes the largest.
/// </summary>
[SupportedOSPlatform("linux")]
internal static partial class X11WindowIcon
{
    private const int PropModeReplace = 0;
    private const nint XaCardinal = 6;
    private const int Size = 64;
    private static readonly Lazy<nint[]> Data = new(CreateData);

    /// <summary>
    /// Sets the icon of the window, which must be open.
    /// </summary>
    public static void Apply(Window window)
    {
        if (window.TryGetPlatformHandle() is not { HandleDescriptor: "XID" } handle)
        {
            return;
        }

        try
        {
            var display = XOpenDisplay(0);
            if (display == 0)
            {
                return;
            }

            try
            {
                var data = Data.Value;
                var property = XInternAtom(display, "_NET_WM_ICON", false);
                XChangeProperty(display, handle.Handle, property, XaCardinal, 32, PropModeReplace, data, data.Length);
                XFlush(display);
            }
            finally
            {
                XCloseDisplay(display);
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            Log.Warning(ex, "Could not set the window icon.");
        }
    }

    /// <summary>
    /// The property's value: width, height and the ARGB pixels, one per C long (format 32).
    /// </summary>
    private static nint[] CreateData()
    {
        using var stream = ShellIcons.OpenIcon();
        using var source = new Bitmap(stream);
        using var scaled = source.CreateScaledBitmap(new PixelSize(Size, Size), BitmapInterpolationMode.HighQuality);
        var pixels = new byte[Size * Size * 4];
        var buffer = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            scaled.CopyPixels(new PixelRect(0, 0, Size, Size), buffer.AddrOfPinnedObject(), pixels.Length, Size * 4);
        }
        finally
        {
            buffer.Free();
        }

        var isRgba = scaled.Format == PixelFormat.Rgba8888;
        var isPremultiplied = scaled.AlphaFormat != AlphaFormat.Unpremul;
        var data = new nint[2 + (Size * Size)];
        data[0] = Size;
        data[1] = Size;
        for (var i = 0; i < pixels.Length; i += 4)
        {
            int alpha = pixels[i + 3];
            int red = pixels[i + (isRgba ? 0 : 2)];
            int green = pixels[i + 1];
            int blue = pixels[i + (isRgba ? 2 : 0)];
            if (isPremultiplied && alpha is > 0 and < 255)
            {
                red = Math.Min(255, ((red * 255) + (alpha / 2)) / alpha);
                green = Math.Min(255, ((green * 255) + (alpha / 2)) / alpha);
                blue = Math.Min(255, ((blue * 255) + (alpha / 2)) / alpha);
            }

            data[2 + (i / 4)] = (alpha << 24) | (red << 16) | (green << 8) | blue;
        }

        return data;
    }

    [LibraryImport("libX11.so.6")]
    private static partial int XChangeProperty(
        nint display,
        nint window,
        nint property,
        nint type,
        int format,
        int mode,
        nint[] data,
        int elements
    );

    [LibraryImport("libX11.so.6")]
    private static partial int XCloseDisplay(nint display);

    [LibraryImport("libX11.so.6")]
    private static partial int XFlush(nint display);

    [LibraryImport("libX11.so.6", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint XInternAtom(
        nint display,
        string name,
        [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists
    );

    [LibraryImport("libX11.so.6")]
    private static partial nint XOpenDisplay(nint name);
}
