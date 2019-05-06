namespace Micser.DriverUtility
{
    public static class DriverGlobals
    {
        public const string DeviceSymLink = @"\\.\Micser.Vac.Driver.Device";
        public const int MaxDeviceCount = 32;

        public static class IoControlCodes
        {
            public const uint Reload = 0x800;
        }

        public static class RegistryValues
        {
            public const string DeviceCount = "DeviceCount";
        }
    }
}