using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Micser.Setup.CustomActions
{
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

        public static bool InstallInfDriver(string filePath, string hardwareID, string description = "")
        {
            filePath = Path.GetFullPath(filePath);

            var classGuid = Guid.Empty;
            var className = new char[MAX_CLASS_NAME_LEN];
            uint requiredSize = 0;
            var deviceInfoData = new SP_DEVINFO_DATA();
            deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

            // Use the INF File to extract the Class GUID
            if (!SetupDiGetINFClass(filePath, ref classGuid, className, MAX_CLASS_NAME_LEN, ref requiredSize))
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
            if (!SetupDiSetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_HARDWAREID, Encoding.Unicode.GetBytes(hardwareID), (uint)Encoding.Unicode.GetByteCount(hardwareID)))
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
            if (!UpdateDriverForPlugAndPlayDevices(IntPtr.Zero, hardwareID, filePath, INSTALLFLAG_FORCE, IntPtr.Zero))
            {
                LogWin32Error(nameof(UpdateDriverForPlugAndPlayDevices));
                return false;
            }

            return true;
        }

        public static bool UninstallDevice(string hardwareID)
        {
            var deviceInfoSet = SetupDiGetClassDevsEx(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, DIGCF_ALLCLASSES, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            if (deviceInfoSet == INVALID_HANDLE_VALUE)
            {
                LogWin32Error(nameof(SetupDiGetClassDevsEx));
                return false;
            }

            uint index = 0;
            var deviceInfoData = new SP_DEVINFO_DATA();
            deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

            while (SetupDiEnumDeviceInfo(deviceInfoSet, index, ref deviceInfoData))
            {
                index++;
                uint requiredSize = 0;
                SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_HARDWAREID, IntPtr.Zero, IntPtr.Zero, 0, ref requiredSize);

                if (requiredSize == 0)
                {
                    continue;
                }

                var pPropertyBuffer = Marshal.AllocHGlobal((int)requiredSize);

                if (!SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_HARDWAREID, IntPtr.Zero, pPropertyBuffer, requiredSize, IntPtr.Zero))
                {
                    continue;
                }

                var currHardwareID = Marshal.PtrToStringAuto(pPropertyBuffer);
                Marshal.FreeHGlobal(pPropertyBuffer);

                if (currHardwareID == hardwareID)
                {
                    var classInstallParams = new SP_REMOVEDEVICE_PARAMS();
                    classInstallParams.ClassInstallHeader.cbSize = (uint)Marshal.SizeOf(classInstallParams.ClassInstallHeader);
                    classInstallParams.ClassInstallHeader.InstallFunction = DIF_REMOVE;
                    classInstallParams.Scope = DI_REMOVEDEVICE_GLOBAL;

                    if (!SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInfoData, ref classInstallParams, (uint)Marshal.SizeOf(classInstallParams)))
                    {
                        LogWin32Error(nameof(SetupDiSetClassInstallParams));
                        return false;
                    }

                    if (!SetupDiCallClassInstaller(DIF_REMOVE, deviceInfoSet, ref deviceInfoData))
                    {
                        LogWin32Error(nameof(SetupDiCallClassInstaller));
                        return false;
                    }
                }
            }

            SetupDiDestroyDeviceInfoList(deviceInfoSet);

            return true;
        }

        private static void LogWin32Error(string method)
        {
            if (Log != null)
            {
                var errorCode = Marshal.GetLastWin32Error();
                Log($"Error: {method} failed with error code {errorCode}.");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_CLASSINSTALL_HEADER
        {
            public uint cbSize;
            public uint InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;

            [MarshalAs(UnmanagedType.Struct)]
            public Guid ClassGuid;

            public int DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_REMOVEDEVICE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader;
            public uint HwProfile;
            public uint Scope;
        }
    }
}