using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Micser.Common;
using NLog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace Micser.DriverUtility
{
    public class DriverController
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public int SetDeviceSettingsAndReload(int deviceCount)
        {
            if (deviceCount < 1)
            {
                deviceCount = 1;
            }

            if (deviceCount > Globals.MaxVacCount)
            {
                deviceCount = Globals.MaxVacCount;
            }

            var currentCount = GetRegistryValue();

            if (!SetRegistryValue(deviceCount))
            {
                return Globals.DriverUtility.ReturnCodes.RegistryAccessFailed;
            }

            try
            {
                var result = Globals.DriverUtility.ReturnCodes.Success;

                var hFileHandle = SafeNativeMethods.CreateFile(
                    DriverGlobals.DeviceSymLink,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite,
                    IntPtr.Zero,
                    FileMode.Open,
                    0,
                    IntPtr.Zero);

                if (hFileHandle.IsInvalid)
                {
                    Logger.Error("Could not open a driver handle.");
                    hFileHandle.Dispose();
                    result = Globals.DriverUtility.ReturnCodes.SendControlSignalFailed;
                }
                else
                {
                    var ioCtlReload = SafeNativeMethods.CtlCode(
                        SafeNativeMethods.FILE_DEVICE_UNKNOWN,
                        DriverGlobals.IoControlCodes.Reload,
                        SafeNativeMethods.METHOD_BUFFERED,
                        SafeNativeMethods.GENERIC_READ | SafeNativeMethods.GENERIC_WRITE);

                    try
                    {
                        Logger.Info($"Sending control code [{DriverGlobals.IoControlCodes.Reload}], encoded: [{ioCtlReload:X}]");

                        uint bytesReturned = 0;
                        var overlapped = new NativeOverlapped();
                        var success = SafeNativeMethods.DeviceIoControl(hFileHandle, ioCtlReload, null, 0, null, 0, ref bytesReturned, ref overlapped);

                        if (!success)
                        {
                            Logger.Error("DeviceIoControl failed.");
                            result = Globals.DriverUtility.ReturnCodes.SendControlSignalFailed;
                        }
                    }
                    finally
                    {
                        if (!hFileHandle.IsClosed && !hFileHandle.IsInvalid)
                        {
                            hFileHandle.Close();
                            hFileHandle.Dispose();
                        }
                    }
                }

                if (result != Globals.DriverUtility.ReturnCodes.Success)
                {
                    SetRegistryValue(currentCount);
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while sending the reload control signal to the driver.");
                SetRegistryValue(currentCount);
                return Globals.DriverUtility.ReturnCodes.SendControlSignalFailed;
            }
        }

        private int GetRegistryValue()
        {
            try
            {
                var registryKey = Registry.CurrentUser.CreateSubKey(Globals.UserRegistryRoot, false);
                return (int)registryKey.GetValue(Globals.RegistryValues.VacCount, 1);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while getting the current device count.");
                return 1;
            }
        }

        private bool SetRegistryValue(int deviceCount)
        {
            try
            {
                var registryKey = Registry.CurrentUser.CreateSubKey(Globals.UserRegistryRoot, true);
                registryKey.SetValue(Globals.RegistryValues.VacCount, (uint)deviceCount, RegistryValueKind.DWord);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not save the settings to the registry.");
                return false;
            }
        }

        [ComVisible(false)]
        [SuppressUnmanagedCodeSecurity]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private class SafeNativeMethods
        {
            public const uint FILE_DEVICE_UNKNOWN = 0x00000022;
            public const uint GENERIC_READ = 0x80000000;
            public const uint GENERIC_WRITE = 0x40000000;
            public const uint METHOD_BUFFERED = 0;

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern SafeFileHandle CreateFile(
                string lpFileName,
                [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
                [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
                IntPtr lpSecurityAttributes,
                [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
                [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
                IntPtr hTemplateFile);

            public static uint CtlCode(uint deviceType, uint function, uint method, uint fileAccess)
            {
                return (deviceType << 16) | (fileAccess << 14) | (function << 2) | method;
            }

            [DllImport("Kernel32.dll", SetLastError = false, CharSet = CharSet.Auto)]
            public static extern bool DeviceIoControl(
                SafeFileHandle hDevice,
                uint IoControlCode,
                [MarshalAs(UnmanagedType.AsAny)]
                [In] object InBuffer,
                uint nInBufferSize,
                [MarshalAs(UnmanagedType.AsAny)]
                [Out] object OutBuffer,
                uint nOutBufferSize,
                ref uint pBytesReturned,
                [In] ref NativeOverlapped Overlapped
            );
        }
    }
}