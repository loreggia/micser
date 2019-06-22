using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Micser.Setup.CustomActions
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class SetupApi
    {
        public const uint DI_REMOVEDEVICE_GLOBAL = 1;
        public const uint DICD_GENERATE_ID = 1;
        public const uint DIF_REGISTERDEVICE = 25;
        public const uint DIF_REMOVE = 5;
        public const uint DIGCF_ALLCLASSES = 4;
        public const uint DIGCF_PRESENT = 2;
        public const uint INSTALLFLAG_FORCE = 1;
        public const uint MAX_CLASS_NAME_LEN = 32;
        public const uint SPDRP_HARDWAREID = 1;
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        public static Action<string> Log = msg => Console.WriteLine(msg);

        #region Imports

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiCallClassInstaller(uint InstallFunction, IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiCreateDeviceInfo(IntPtr DeviceInfoSet, string ClassName, ref Guid ClassGuid, string DeviceDescription, IntPtr hwndParent, uint CreationFlags, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetupDiCreateDeviceInfoList(ref Guid ClassGuid, IntPtr hwndParent);

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevsEx(IntPtr ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags, IntPtr DeviceInfoSet, IntPtr MachineName, IntPtr Reserved);

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiGetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, uint Property, IntPtr PropertyRegDataType, IntPtr PropertyBuffer, uint PropertyBufferSize, IntPtr RequiredSize);

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiGetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, uint Property, IntPtr PropertyRegDataType, IntPtr PropertyBuffer, uint PropertyBufferSize, ref uint RequiredSize);

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiGetINFClass(string InfName, ref Guid ClassGuid, [Out] char[] ClassName, uint ClassNameSize, ref uint RequiredSize);

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiSetClassInstallParams(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, ref SP_REMOVEDEVICE_PARAMS ClassInstallParams, uint ClassInstallParamsSize);

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiSetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, uint Property, byte[] PropertyBuffer, uint PropertyBufferSize);

        [DllImport("Newdev.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool UpdateDriverForPlugAndPlayDevices(IntPtr hwndParent, string HardwareId, string FullInfPath, uint InstallFlags, IntPtr pRebootRequired);

        #endregion Imports

        public static bool GetDevice(string hardwareId, out IntPtr deviceInfoList, out SP_DEVINFO_DATA? deviceInfoData)
        {
            deviceInfoData = null;

            deviceInfoList = SetupDiGetClassDevsEx(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, DIGCF_ALLCLASSES, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            if (deviceInfoList == INVALID_HANDLE_VALUE)
            {
                LogWin32Error(nameof(SetupDiGetClassDevsEx));
                return false;
            }

            var currentDeviceInfoData = new SP_DEVINFO_DATA();
            currentDeviceInfoData.cbSize = (uint)Marshal.SizeOf(currentDeviceInfoData);

            for (uint i = 0; SetupDiEnumDeviceInfo(deviceInfoList, i, ref currentDeviceInfoData); i++)
            {
                uint requiredSize = 0;
                SetupDiGetDeviceRegistryProperty(deviceInfoList, ref currentDeviceInfoData, SPDRP_HARDWAREID, IntPtr.Zero, IntPtr.Zero, 0, ref requiredSize);

                if (requiredSize == 0)
                {
                    continue;
                }

                var pPropertyBuffer = Marshal.AllocHGlobal((int)requiredSize);

                if (!SetupDiGetDeviceRegistryProperty(deviceInfoList, ref currentDeviceInfoData, SPDRP_HARDWAREID, IntPtr.Zero, pPropertyBuffer, requiredSize, IntPtr.Zero))
                {
                    continue;
                }

                var currentHardwareId = Marshal.PtrToStringAuto(pPropertyBuffer);
                Marshal.FreeHGlobal(pPropertyBuffer);

                if (currentHardwareId == hardwareId)
                {
                    deviceInfoData = currentDeviceInfoData;
                    return true;
                }
            }

            return false;
        }

        public static bool InstallDevice(string infFilePath, string hardwareId)
        {
            infFilePath = Path.GetFullPath(infFilePath);

            var classGuid = Guid.Empty;
            var className = new char[MAX_CLASS_NAME_LEN];
            var description = "";
            uint requiredSize = 0;
            var deviceInfoData = new SP_DEVINFO_DATA();
            deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

            if (IsInstalled(hardwareId))
            {
                LogMessage($"Device with hardware ID '{hardwareId}' is already installed.");
                return true;
            }

            // Use the INF File to extract the Class GUID
            if (!SetupDiGetINFClass(infFilePath, ref classGuid, className, MAX_CLASS_NAME_LEN, ref requiredSize))
            {
                LogWin32Error(nameof(SetupDiGetINFClass));
                return false;
            }

            // Create the container for the to-be-created Device Information Element
            var deviceInfoSet = SetupDiCreateDeviceInfoList(ref classGuid, IntPtr.Zero);

            if (deviceInfoSet == INVALID_HANDLE_VALUE)
            {
                LogWin32Error(nameof(SetupDiCreateDeviceInfoList));
                return false;
            }

            // Now create the element. Use the Class GUID and Name from the INF file
            if (!SetupDiCreateDeviceInfo(deviceInfoSet, new string(className), ref classGuid, description, IntPtr.Zero, DICD_GENERATE_ID, ref deviceInfoData))
            {
                LogWin32Error(nameof(SetupDiCreateDeviceInfo));
                return false;
            }

            // Add the HardwareID to the Device's HardwareID property
            if (!SetupDiSetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_HARDWAREID, Encoding.Unicode.GetBytes(hardwareId), (uint)Encoding.Unicode.GetByteCount(hardwareId)))
            {
                LogWin32Error(nameof(SetupDiSetDeviceRegistryProperty));
                return false;
            }

            // Transform the registry element into an actual devnode in the PnP HW tree
            if (!SetupDiCallClassInstaller(DIF_REGISTERDEVICE, deviceInfoSet, ref deviceInfoData))
            {
                LogWin32Error(nameof(SetupDiCallClassInstaller));
                return false;
            }

            SetupDiDestroyDeviceInfoList(deviceInfoSet);

            // Update the driver for the device we just created
            if (!UpdateDriverForPlugAndPlayDevices(IntPtr.Zero, hardwareId, infFilePath, INSTALLFLAG_FORCE, IntPtr.Zero))
            {
                LogWin32Error(nameof(UpdateDriverForPlugAndPlayDevices));
                return false;
            }

            return true;
        }

        public static bool IsInstalled(string hardwareId)
        {
            var result = GetDevice(hardwareId, out var deviceInfoList, out var deviceInfoData) && deviceInfoData.HasValue;
            if (deviceInfoList != INVALID_HANDLE_VALUE)
            {
                SetupDiDestroyDeviceInfoList(deviceInfoList);
            }
            return result;
        }

        public static bool UninstallDevice(string hardwareId)
        {
            var result = true;

            if (!GetDevice(hardwareId, out var deviceInfoList, out var deviceInfoData) || deviceInfoData == null)
            {
                LogMessage($"No device for hardware ID '{hardwareId}' found.");
            }
            else
            {
                var deviceInfoDataValue = deviceInfoData.Value;

                var removeParams = new SP_REMOVEDEVICE_PARAMS();
                removeParams.ClassInstallHeader.cbSize = (uint)Marshal.SizeOf(removeParams.ClassInstallHeader);
                removeParams.ClassInstallHeader.InstallFunction = DIF_REMOVE;
                removeParams.Scope = DI_REMOVEDEVICE_GLOBAL;

                if (!SetupDiSetClassInstallParams(deviceInfoList, ref deviceInfoDataValue, ref removeParams, (uint)Marshal.SizeOf(removeParams)))
                {
                    LogWin32Error(nameof(SetupDiSetClassInstallParams));
                    result = false;
                }
                else if (!SetupDiCallClassInstaller(DIF_REMOVE, deviceInfoList, ref deviceInfoDataValue))
                {
                    LogWin32Error(nameof(SetupDiCallClassInstaller));
                    result = false;
                }
            }

            if (deviceInfoList != INVALID_HANDLE_VALUE)
            {
                SetupDiDestroyDeviceInfoList(deviceInfoList);
            }

            return result;
        }

        private static void LogMessage(string msg)
        {
            Log?.Invoke(msg);
        }

        private static void LogWin32Error(string method)
        {
            if (Log != null)
            {
                var errorCode = Marshal.GetLastWin32Error();
                Log($"Error: {method} failed with error code {errorCode}.");
            }
        }

#if X64

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
#else
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
#endif
        public struct SP_CLASSINSTALL_HEADER
        {
            public uint cbSize;
            public uint InstallFunction;
        }

#if X64

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
#else
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
#endif
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;

            [MarshalAs(UnmanagedType.Struct)]
            public Guid ClassGuid;

            public int DevInst;
            public IntPtr Reserved;
        }

#if X64

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
#else
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
#endif
        public struct SP_REMOVEDEVICE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader;
            public uint Scope;
            public uint HwProfile;
        }
    }
}