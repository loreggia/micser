using CSCore.Win32;
using System;

namespace Micser.DriverUtility
{
    public static class DriverGlobals
    {
        public const string DeviceSymLink = @"\\.\Micser.Vac.Driver.Device";

        public static class IoControlCodes
        {
            public const uint Reload = 0x800;
        }

        public static class PropertyKeys
        {
            public static readonly PropertyKey DeviceDescription = new PropertyKey(new Guid("{a45c254e-df1c-4efd-8020-67d146a850e0}"), 2);
            public static readonly PropertyKey DeviceName = new PropertyKey(new Guid("b3f8fa53-0004-438e-9003-51a46e139bfc"), 6);
            public static readonly PropertyKey TopologyInfo = new PropertyKey(new Guid("{233164c8-1b2c-4c7d-bc68-b671687a2567}"), 1);
        }
    }
}