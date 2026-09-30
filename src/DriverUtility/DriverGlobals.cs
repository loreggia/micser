using NAudio.CoreAudioApi;

namespace Micser.DriverUtility;

internal static class DriverGlobals
{
    public const string DeviceCountValue = "DeviceCount";
    public const string DeviceInterfaceName = "Micser Virtual Audio Cable";
    public const string DeviceSymLink = @"\\.\Micser.Vac.Driver.Device";
    public const int MaxDeviceCount = 8;
    public const string RegistryKey = @"Software\Micser";
    public const uint ReloadControlCode = 0x800;

    /// <summary>
    /// PKEY_Device_DeviceDesc: the endpoint name shown in Windows, e.g. "Speakers".
    /// </summary>
    public static readonly PropertyKey DeviceDescriptionKey = new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 2);

    /// <summary>
    /// Undocumented; holds the name of the device interface the endpoint belongs to.
    /// </summary>
    public static readonly PropertyKey DeviceInterfaceNameKey = new(new Guid("b3f8fa53-0004-438e-9003-51a46e139bfc"), 6);

    /// <summary>
    /// Undocumented; holds the endpoint's topology path, which ends in the driver's wave filter index.
    /// </summary>
    public static readonly PropertyKey TopologyInfoKey = new(new Guid("233164c8-1b2c-4c7d-bc68-b671687a2567"), 1);
}

internal static class ReturnCodes
{
    public const int InvalidParameter = -2;
    public const int RegistryAccessFailed = -10;
    public const int RequiresAdminAccess = -3;
    public const int SendControlSignalFailed = -11;
    public const int Success = 0;
    public const int UnknownError = -1;
}
