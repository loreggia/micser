using System.Runtime.InteropServices;

namespace Micser.DriverUtility;

/// <summary>
/// SetupAPI, newdev, cfgmgr32 and kernel32 functions for installing, managing and controlling the cable driver.
/// </summary>
internal static unsafe partial class NativeMethods
{
    public const uint CR_SUCCESS = 0;
    public const uint DICD_GENERATE_ID = 0x00000001;
    public const uint DICS_FLAG_GLOBAL = 0x00000001;
    public const uint DIF_REGISTERDEVICE = 0x00000019;
    public const uint DIF_REMOVE = 0x00000005;
    public const uint DIGCF_ALLCLASSES = 0x00000004;
    public const uint DIREG_DEV = 0x00000001;
    public const uint DN_HAS_PROBLEM = 0x00000400;
    public const uint DN_STARTED = 0x00000008;
    public const int ERROR_BUSY = 170;
    public const int ERROR_FILE_NOT_FOUND = 2;
    public const int ERROR_INVALID_DATA = 13;
    public const int ERROR_NO_MORE_ITEMS = 259;
    public const uint FILE_SHARE_READ_WRITE = 0x00000003;
    public const uint GENERIC_READ_WRITE = 0xC0000000;
    public const uint INSTALLFLAG_FORCE = 0x00000001;
    public const uint KEY_READ = 0x00020019;
    public const uint OPEN_EXISTING = 3;
    public const uint SPDRP_HARDWAREID = 0x00000001;
    public const uint SUOI_FORCEDELETE = 0x00000001;

    public static readonly nint InvalidHandle = -1;

    /// <summary>
    /// DEVPKEY_Device_DriverInfPath: the published name of the installed INF, e.g. oem7.inf.
    /// </summary>
    public static readonly DEVPROPKEY DriverInfPathKey = new(new Guid("a8b865dd-2e3d-4094-ad97-e593a70c75d6"), 5);

    /// <summary>
    /// DEVPKEY_Device_Parent: the parent's instance ID, e.g. a cable device's for an endpoint.
    /// </summary>
    public static readonly DEVPROPKEY ParentKey = new(new Guid("4340a6c5-93fa-4706-972c-7b648008a5a7"), 8);

    /// <summary>
    /// DEVPKEY_Device_DriverVersion
    /// </summary>
    public static readonly DEVPROPKEY DriverVersionKey = new(new Guid("a8b865dd-2e3d-4094-ad97-e593a70c75d6"), 3);

    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "CreateFileW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16
    )]
    public static partial Microsoft.Win32.SafeHandles.SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        nint securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        nint templateFile
    );

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeviceIoControl(
        Microsoft.Win32.SafeHandles.SafeFileHandle device,
        uint ioControlCode,
        void* inBuffer,
        uint inBufferSize,
        void* outBuffer,
        uint outBufferSize,
        out uint bytesReturned,
        nint overlapped
    );

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

    [LibraryImport("setupapi.dll", SetLastError = true)]
    public static partial nint SetupDiCreateDeviceInfoList(nint classGuid, nint hwndParent);

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
    public struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public nint Reserved;
    }
}
