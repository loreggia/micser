using CSCore.Win32;
using System;

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

        public static class PropertyKeys
        {
            public static readonly PropertyKey DeviceDescription = new PropertyKey(new Guid("{a45c254e-df1c-4efd-8020-67d146a850e0}"), 2);
            public static readonly PropertyKey TopologyInfo = new PropertyKey(new Guid("{233164c8-1b2c-4c7d-bc68-b671687a2567}"), 1);
        }

        public static class RegistryValues
        {
            public const string DeviceCount = "DeviceCount";
        }
    }
}