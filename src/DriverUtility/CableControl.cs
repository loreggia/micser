using System.ComponentModel;
using System.Runtime.InteropServices;
using static Micser.DriverUtility.NativeMethods;

namespace Micser.DriverUtility;

/// <summary>
/// The cable configuration of the running driver, through its control device (src/CableDriver/public.h). Any signed-in user may change
/// it; the driver keeps it in the root device's hardware key and adds, removes and re-creates the cable devices right away.
/// </summary>
internal static unsafe class CableControl
{
    public const string DevicePath = @"\\.\MicserCable";

    // CTL_CODE(0x8000, 0x800, METHOD_BUFFERED, FILE_READ_ACCESS) and CTL_CODE(0x8000, 0x801, METHOD_BUFFERED, FILE_WRITE_ACCESS)
    public const uint GetConfigCode = 0x80006000;
    public const uint SetConfigCode = 0x8000A004;

    /// <summary>
    /// The configuration, or null when the driver doesn't run.
    /// </summary>
    public static CableConfig? TryGet()
    {
        using var device = Open();
        if (device == null)
        {
            return null;
        }

        var config = default(MICSERCABLE_CONFIG);
        if (!DeviceIoControl(device, GetConfigCode, null, 0, &config, (uint)sizeof(MICSERCABLE_CONFIG), out _, 0))
        {
            throw LastError("Reading the cable configuration");
        }

        return CableConfig.FromNative(config);
    }

    /// <summary>
    /// Applies a configuration.
    /// </summary>
    public static CableSetResult Set(CableConfig config)
    {
        using var device = Open();
        if (device == null)
        {
            return CableSetResult.NotRunning;
        }

        var native = config.ToNative();
        if (!DeviceIoControl(device, SetConfigCode, &native, (uint)sizeof(MICSERCABLE_CONFIG), null, 0, out _, 0))
        {
            // STATUS_DEVICE_BUSY: an app still has a cable's previous device open; the driver adds the cable once it is closed
            if (Marshal.GetLastPInvokeError() == ERROR_BUSY)
            {
                return CableSetResult.Pending;
            }

            throw LastError("Applying the cable configuration");
        }

        return CableSetResult.Applied;
    }

    private static Microsoft.Win32.SafeHandles.SafeFileHandle? Open()
    {
        var device = CreateFile(DevicePath, GENERIC_READ_WRITE, FILE_SHARE_READ_WRITE, 0, OPEN_EXISTING, 0, 0);
        if (!device.IsInvalid)
        {
            return device;
        }

        var error = Marshal.GetLastPInvokeError();
        device.Dispose();
        if (error == ERROR_FILE_NOT_FOUND)
        {
            return null;
        }

        throw new Win32Exception(error, $"Opening {DevicePath} failed: {new Win32Exception(error).Message}");
    }

    private static Win32Exception LastError(string action)
    {
        var error = Marshal.GetLastPInvokeError();
        return new Win32Exception(error, $"{action} failed: {new Win32Exception(error).Message}");
    }
}

internal enum CableSetResult
{
    Applied,

    /// <summary>
    /// The driver doesn't run.
    /// </summary>
    NotRunning,

    /// <summary>
    /// Applied, but some cables come back only once the apps that use their previous devices close them.
    /// </summary>
    Pending,
}

/// <summary>
/// MICSERCABLE_CONFIG in src/CableDriver/public.h.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct MICSERCABLE_CONFIG
{
    public uint CableCount;
    public fixed uint Channels[CableDriverDevice.MaxCableCount];
    public uint ActiveCables;
}

/// <summary>
/// The cables: <see cref="CableCount"/> cables, cable N with layout <c>Layouts[N - 1]</c>. <see cref="Layouts"/> has an entry for every
/// possible cable, so cables above the count keep their layout. <see cref="ActiveCables"/> has bit N - 1 set while cable N's device
/// exists.
/// </summary>
internal sealed record CableConfig(int CableCount, IReadOnlyList<CableLayout> Layouts, uint ActiveCables = 0)
{
    public static unsafe CableConfig FromNative(MICSERCABLE_CONFIG config)
    {
        var layouts = new CableLayout[CableDriverDevice.MaxCableCount];
        for (var i = 0; i < layouts.Length; i++)
        {
            layouts[i] = CableLayouts.FromChannels((int)config.Channels[i]);
        }

        return new CableConfig(
            Math.Clamp((int)config.CableCount, 1, CableDriverDevice.MaxCableCount),
            layouts,
            config.ActiveCables
        );
    }

    /// <summary>
    /// The layouts of the cables up to the count.
    /// </summary>
    public IReadOnlyList<CableLayout> ActiveLayouts => [.. Layouts.Take(CableCount)];

    /// <summary>
    /// Whether cable N's device exists.
    /// </summary>
    public bool Exists(int cable)
    {
        return (ActiveCables & (1u << (cable - 1))) != 0;
    }

    public unsafe MICSERCABLE_CONFIG ToNative()
    {
        var config = new MICSERCABLE_CONFIG { CableCount = (uint)CableCount };
        for (var i = 0; i < CableDriverDevice.MaxCableCount; i++)
        {
            config.Channels[i] = (uint)(i < Layouts.Count ? Layouts[i] : CableLayout.Stereo);
        }

        return config;
    }
}
