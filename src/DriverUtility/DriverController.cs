using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Serilog;

namespace Micser.DriverUtility;

/// <summary>
/// Stores the number of virtual devices and tells the driver to reload.
/// </summary>
internal static partial class DriverController
{
    private const uint FileDeviceUnknown = 0x00000022;
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint MethodBuffered = 0;
    private const uint OpenExisting = 3;

    public static int SetDeviceCountAndReload(int deviceCount)
    {
        deviceCount = Math.Clamp(deviceCount, 1, DriverGlobals.MaxDeviceCount);

        var previousCount = GetDeviceCount();
        if (!SetDeviceCount(deviceCount))
        {
            return ReturnCodes.RegistryAccessFailed;
        }

        if (!SendReload())
        {
            SetDeviceCount(previousCount);
            return ReturnCodes.SendControlSignalFailed;
        }

        return ReturnCodes.Success;
    }

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode, nint securityAttributes, uint creationDisposition, uint flagsAndAttributes, nint templateFile);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeviceIoControl(SafeFileHandle device, uint ioControlCode, nint inBuffer, uint inBufferSize, nint outBuffer, uint outBufferSize, out uint bytesReturned, nint overlapped);

    private static int GetDeviceCount()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(DriverGlobals.RegistryKey, false);
            return key.GetValue(DriverGlobals.DeviceCountValue) is int count ? count : 1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not read the current device count.");
            return 1;
        }
    }

    private static bool SendReload()
    {
        using var handle = CreateFile(DriverGlobals.DeviceSymLink, GenericRead | GenericWrite, (uint)(FileShare.ReadWrite), 0, OpenExisting, 0, 0);
        if (handle.IsInvalid)
        {
            Log.Error("Could not open the driver (Win32 error {Error}).", Marshal.GetLastPInvokeError());
            return false;
        }

        var controlCode = (FileDeviceUnknown << 16) | ((GenericRead | GenericWrite) << 14) | (DriverGlobals.ReloadControlCode << 2) | MethodBuffered;
        Log.Information("Sending control code {Function:X} ({ControlCode:X}).", DriverGlobals.ReloadControlCode, controlCode);

        if (!DeviceIoControl(handle, controlCode, 0, 0, 0, 0, out _, 0))
        {
            Log.Error("DeviceIoControl failed (Win32 error {Error}).", Marshal.GetLastPInvokeError());
            return false;
        }

        return true;
    }

    private static bool SetDeviceCount(int deviceCount)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(DriverGlobals.RegistryKey, true);
            key.SetValue(DriverGlobals.DeviceCountValue, deviceCount, RegistryValueKind.DWord);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not save the device count to the registry.");
            return false;
        }
    }
}
