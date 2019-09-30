namespace Micser.DriverUtility
{
    public static class DriverGlobals
    {
        public const string DeviceSymLink = @"\\.\Micser.Vac.Driver.Device";

        public static class IoControlCodes
        {
            public const uint Reload = 0x800;
        }
    }
}