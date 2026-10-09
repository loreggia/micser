using System.Runtime.InteropServices;

namespace Micser.DriverUtility;

/// <summary>
/// SetupAPI, newdev and cfgmgr32 functions for installing and managing the root-enumerated VAC device.
/// </summary>
internal static unsafe partial class NativeMethods
{
    public const uint CR_SUCCESS = 0;
    public const uint DI_NEEDREBOOT = 0x00000100;
    public const uint DI_NEEDRESTART = 0x00000080;
    public const uint DICD_GENERATE_ID = 0x00000001;
    public const uint DICS_FLAG_GLOBAL = 0x00000001;
    public const uint DICS_PROPCHANGE = 0x00000003;
    public const uint DIF_PROPERTYCHANGE = 0x00000012;
    public const uint DIF_REGISTERDEVICE = 0x00000019;
    public const uint DIF_REMOVE = 0x00000005;
    public const uint DIGCF_ALLCLASSES = 0x00000004;
    public const uint DIREG_DEV = 0x00000001;
    public const uint DN_HAS_PROBLEM = 0x00000400;
    public const uint DN_STARTED = 0x00000008;
    public const int ERROR_NO_MORE_ITEMS = 259;
    public const uint INSTALLFLAG_FORCE = 0x00000001;
    public const uint KEY_READ = 0x00020019;
    public const uint KEY_SET_VALUE = 0x00000002;
    public const uint SPDRP_HARDWAREID = 0x00000001;
    public const uint SUOI_FORCEDELETE = 0x00000001;

    public static readonly nint InvalidHandle = -1;

    /// <summary>
    /// DEVPKEY_Device_DriverInfPath: the published name of the installed INF, e.g. oem7.inf.
    /// </summary>
    public static readonly DEVPROPKEY DriverInfPathKey = new(new Guid("a8b865dd-2e3d-4094-ad97-e593a70c75d6"), 5);

    /// <summary>
    /// DEVPKEY_Device_InstanceId, e.g. ROOT\MEDIA\0000.
    /// </summary>
    public static readonly DEVPROPKEY InstanceIdKey = new(new Guid("78c34fc8-104a-4aca-9ea4-524d52996e57"), 256);

    /// <summary>
    /// DEVPKEY_Device_DriverVersion
    /// </summary>
    public static readonly DEVPROPKEY DriverVersionKey = new(new Guid("a8b865dd-2e3d-4094-ad97-e593a70c75d6"), 3);

    [LibraryImport("cfgmgr32.dll")]
    public static partial uint CM_Get_DevNode_Status(out uint status, out uint problemNumber, uint devInst, uint flags);

    [LibraryImport("newdev.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DiUninstallDevice(
        nint hwndParent,
        nint deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint flags,
        [MarshalAs(UnmanagedType.Bool)] out bool needReboot
    );

    [LibraryImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiCallClassInstaller(
        uint installFunction,
        nint deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData
    );

    [LibraryImport(
        "setupapi.dll",
        EntryPoint = "SetupDiCreateDeviceInfoW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16
    )]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiCreateDeviceInfo(
        nint deviceInfoSet,
        string deviceName,
        ref Guid classGuid,
        string? deviceDescription,
        nint hwndParent,
        uint creationFlags,
        ref SP_DEVINFO_DATA deviceInfoData
    );

    [LibraryImport("setupapi.dll", SetLastError = true)]
    public static partial nint SetupDiCreateDeviceInfoList(ref Guid classGuid, nint hwndParent);

    [LibraryImport(
        "setupapi.dll",
        EntryPoint = "SetupDiCreateDevRegKeyW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16
    )]
    public static partial nint SetupDiCreateDevRegKey(
        nint deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint scope,
        uint hwProfile,
        uint keyType,
        nint infHandle,
        string? infSectionName
    );

    [LibraryImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiDestroyDeviceInfoList(nint deviceInfoSet);

    [LibraryImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiEnumDeviceInfo(
        nint deviceInfoSet,
        uint memberIndex,
        ref SP_DEVINFO_DATA deviceInfoData
    );

    [LibraryImport(
        "setupapi.dll",
        EntryPoint = "SetupDiGetClassDevsW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16
    )]
    public static partial nint SetupDiGetClassDevs(nint classGuid, string? enumerator, nint hwndParent, uint flags);

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceInstallParamsW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiGetDeviceInstallParams(
        nint deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        ref SP_DEVINSTALL_PARAMS deviceInstallParams
    );

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiGetDevicePropertyW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiGetDeviceProperty(
        nint deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        in DEVPROPKEY propertyKey,
        out uint propertyType,
        byte* propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize,
        uint flags
    );

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceRegistryPropertyW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiGetDeviceRegistryProperty(
        nint deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        byte* propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize
    );

    [LibraryImport(
        "setupapi.dll",
        EntryPoint = "SetupDiGetINFClassW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16
    )]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiGetINFClass(
        string infName,
        out Guid classGuid,
        char* className,
        uint classNameSize,
        out uint requiredSize
    );

    [LibraryImport("setupapi.dll", SetLastError = true)]
    public static partial nint SetupDiOpenDevRegKey(
        nint deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint scope,
        uint hwProfile,
        uint keyType,
        uint samDesired
    );

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiSetClassInstallParamsW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiSetClassInstallParams(
        nint deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        ref SP_PROPCHANGE_PARAMS classInstallParams,
        uint classInstallParamsSize
    );

    [LibraryImport("setupapi.dll", EntryPoint = "SetupDiSetDeviceRegistryPropertyW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiSetDeviceRegistryProperty(
        nint deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint property,
        byte* propertyBuffer,
        uint propertyBufferSize
    );

    [LibraryImport(
        "setupapi.dll",
        EntryPoint = "SetupUninstallOEMInfW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16
    )]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupUninstallOEMInf(string infFileName, uint flags, nint reserved);

    [LibraryImport(
        "newdev.dll",
        EntryPoint = "UpdateDriverForPlugAndPlayDevicesW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16
    )]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UpdateDriverForPlugAndPlayDevices(
        nint hwndParent,
        string hardwareId,
        string fullInfPath,
        uint installFlags,
        [MarshalAs(UnmanagedType.Bool)] out bool rebootRequired
    );

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct DEVPROPKEY
    {
        public readonly Guid FmtId;
        public readonly uint Pid;

        public DEVPROPKEY(Guid fmtId, uint pid)
        {
            FmtId = fmtId;
            Pid = pid;
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
        public Guid ClassGuid;
        public uint DevInst;
        public nint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINSTALL_PARAMS
    {
        public uint cbSize;
        public uint Flags;
        public uint FlagsEx;
        public nint hwndParent;
        public nint InstallMsgHandler;
        public nint InstallMsgHandlerContext;
        public nint FileQueue;
        public nuint ClassInstallReserved;
        public uint Reserved;
        public fixed ushort DriverPath[260];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_PROPCHANGE_PARAMS
    {
        public SP_CLASSINSTALL_HEADER ClassInstallHeader;
        public uint StateChange;
        public uint Scope;
        public uint HwProfile;
    }
}
