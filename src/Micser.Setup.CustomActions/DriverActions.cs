using Microsoft.Deployment.WindowsInstaller;
using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Micser.Setup.CustomActions
{
    public class DriverActions
    {
        private const string HardwareId = @"Root\Micser.Vac.Driver";

        [CustomAction]
        public static ActionResult Configure(Session session)
        {
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult Install(Session session)
        {
            var infPath = GetInfPath(session);
            var result = InstallInternal(infPath, HardwareId);
            return result ? ActionResult.Success : ActionResult.Failure;
        }

        [CustomAction]
        public static ActionResult Uninstall(Session session)
        {
            var infPath = GetInfPath(session);
            var result = UninstallInternal(infPath, HardwareId);
            return result ? ActionResult.Success : ActionResult.Failure;
        }

        private static bool GetDevice(IntPtr deviceInfoList, Guid classGuid, string hwid, out NativeMethods.SP_DEVINFO_DATA deviceInfoData)
        {
            deviceInfoData = new NativeMethods.SP_DEVINFO_DATA
            {
                cbSize = Marshal.SizeOf(typeof(NativeMethods.SP_DEVINFO_DATA))
            };

            var devices = NativeMethods.SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, IntPtr.Zero, NativeMethods.DIGCF_PRESENT);

            if ((long)devices < 0)
            {
                return false;
            }

            for (var i = 0; NativeMethods.SetupDiEnumDeviceInfo(deviceInfoList, (uint)i, ref deviceInfoData); i++)
            {
                var ptrBuffer = IntPtr.Zero;
                const int BUFFER_SIZE = 8192;

                try
                {
                    ptrBuffer = Marshal.AllocHGlobal(BUFFER_SIZE);

                    if (NativeMethods.SetupDiGetDeviceRegistryProperty(deviceInfoList, ref deviceInfoData, NativeMethods.SPDRP_HARDWAREID, out var regType, ptrBuffer, BUFFER_SIZE, out var requiredSize))
                    {
                        var deviceHardwareId = Marshal.PtrToStringAuto(ptrBuffer, (int)requiredSize);

                        if (string.Equals(deviceHardwareId, hwid, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                finally
                {
                    if (ptrBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptrBuffer);
                    }
                }
            }

            return false;
        }

        private static bool GetDeviceInfoList(string inf, out IntPtr deviceInfoList, out string className, out Guid classGuid)
        {
            var sbClassName = new StringBuilder(NativeMethods.MAX_CLASS_NAME_LEN);
            classGuid = Guid.Empty;

            if (!NativeMethods.SetupDiGetINFClass(inf, ref classGuid, sbClassName, NativeMethods.MAX_CLASS_NAME_LEN, 0))
            {
                deviceInfoList = default;
                className = null;
                return false;
            }

            deviceInfoList = NativeMethods.SetupDiCreateDeviceInfoList(ref classGuid, IntPtr.Zero);
            className = sbClassName.ToString();
            return true;
        }

        private static string GetInfPath(Session session)
        {
            var installDir = session["INSTALLDIR"];
            return Path.Combine(installDir, "Micser.Vac.Driver.inf");
        }

        [HandleProcessCorruptedStateExceptions]
        private static bool InstallInternal(string inf, string hwid)
        {
            var deviceInfoList = IntPtr.Zero;

            try
            {
                if (!GetDeviceInfoList(inf, out deviceInfoList, out var className, out var classGuid))
                {
                    return false;
                }

                var deviceInfoData = new NativeMethods.SP_DEVINFO_DATA
                {
                    cbSize = Marshal.SizeOf(typeof(NativeMethods.SP_DEVINFO_DATA))
                };

                if (!NativeMethods.SetupDiCreateDeviceInfo(deviceInfoList, className, ref classGuid, null, IntPtr.Zero, NativeMethods.DICD_GENERATE_ID, ref deviceInfoData))
                {
                    return false;
                }

                if (!NativeMethods.SetupDiSetDeviceRegistryProperty(deviceInfoList, ref deviceInfoData, NativeMethods.SPDRP_HARDWAREID, hwid, hwid.Length))
                {
                    NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoList);
                    return false;
                }

                if (!NativeMethods.SetupDiCallClassInstaller(NativeMethods.DIF_REGISTERDEVICE, deviceInfoList, ref deviceInfoData))
                {
                    NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoList);
                    return false;
                }

                // http://stackoverflow.com/questions/11474317/updatedriverforplugandplaydevices-error-is-telling-me-im-not-doing-something
                try
                {
                    bool reboot = false;
                    if (!NativeMethods.UpdateDriverForPlugAndPlayDevices(IntPtr.Zero, hwid, inf, 0, reboot))
                    {
                        NativeMethods.SetupDiCallClassInstaller(NativeMethods.DIF_REMOVE, deviceInfoList, ref deviceInfoData);
                        return false;
                    }
                }
                catch (AccessViolationException) { }

                return true;
            }
            finally
            {
                if (deviceInfoList != IntPtr.Zero)
                {
                    NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoList);
                }
            }
        }

        private static bool UninstallInternal(string inf, string hwid)
        {
            var deviceInfoList = IntPtr.Zero;

            try
            {
                if (!GetDeviceInfoList(inf, out deviceInfoList, out var className, out var classGuid))
                {
                    return false;
                }

                if (!GetDevice(deviceInfoList, classGuid, hwid, out var deviceInfoData))
                {
                    return false;
                }

                var removeParams = new NativeMethods.SP_REMOVEDEVICE_PARAMS
                {
                    cbSize = sizeof(ulong) + sizeof(uint),
                    InstallFunction = NativeMethods.DIF_REMOVE,
                    Scope = NativeMethods.DI_REMOVEDEVICE_GLOBAL,
                    HwProfile = 0
                };

                if (!NativeMethods.SetupDiSetClassInstallParams(deviceInfoList, ref deviceInfoData, ref removeParams, Marshal.SizeOf(typeof(NativeMethods.SP_REMOVEDEVICE_PARAMS))) ||
                    !NativeMethods.SetupDiCallClassInstaller(NativeMethods.DIF_REMOVE, deviceInfoList, ref deviceInfoData))
                {
                    return false;
                }

                // TODO check if restart is necessary?

                return true;
            }
            finally
            {
                if (deviceInfoList != IntPtr.Zero)
                {
                    NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoList);
                }
            }
        }

        private static class NativeMethods
        {
            public const int DI_REMOVEDEVICE_GLOBAL = 0x00000001;
            public const int DICD_GENERATE_ID = 0x00000001;
            public const int DIF_REGISTERDEVICE = 0x00000019;
            public const int DIF_REMOVE = 0x00000005;
            public const int DIGCF_PRESENT = 0x2;
            public const int MAX_CLASS_NAME_LEN = 32;
            public const int SPDRP_HARDWAREID = 0x00000001;

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiCallClassInstaller(UInt32 InstallFunction, IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData);

            [DllImport("Setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiCreateDeviceInfo(
                IntPtr DeviceInfoSet,
                String DeviceName,
                ref Guid ClassGuid,
                string DeviceDescription,
                IntPtr hwndParent,
                Int32 CreationFlags,
                ref SP_DEVINFO_DATA DeviceInfoData);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern IntPtr SetupDiCreateDeviceInfoList(ref Guid ClassGuid, IntPtr hwndParent);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiGetDeviceRegistryProperty(
                IntPtr deviceInfoSet,
                ref SP_DEVINFO_DATA deviceInfoData,
                uint property,
                out UInt32 propertyRegDataType,
                IntPtr propertyBuffer,
                uint propertyBufferSize,
                out UInt32 requiredSize);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiGetINFClass(string infName, ref Guid ClassGuid, [MarshalAs(UnmanagedType.LPStr)] StringBuilder ClassName, int ClassNameSize, int RequiredSize);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiSetClassInstallParams(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, ref SP_REMOVEDEVICE_PARAMS ClassInstallParams, int ClassInstallParamsSize);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiSetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, uint Property, string PropertyBuffer, int PropertyBufferSize);

            [DllImport("newdev.dll", SetLastError = true)]
            public static extern bool UpdateDriverForPlugAndPlayDevices(IntPtr hwndParent, string HardwareId, string FullInfPath, int InstallFlags, bool bRebootRequired);

            [StructLayout(LayoutKind.Sequential, Pack = 8)]
            public struct SP_DEVINFO_DATA
            {
                public int cbSize;

                [MarshalAs(UnmanagedType.Struct)]
                public Guid ClassGuid;

                public int devInst;
                public long reserved;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 8)]
            public struct SP_REMOVEDEVICE_PARAMS
            {
                public ulong cbSize;
                public uint InstallFunction;
                public ulong Scope;
                public ulong HwProfile;
            }
        }
    }
}