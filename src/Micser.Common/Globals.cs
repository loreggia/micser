using System;
using System.IO;

namespace Micser.Common
{
    public static class Globals
    {
        public const int ApiPort = 60666;

        public const string DeviceInterfaceName = "Micser Virtual Audio Cable";

        public const string PluginSearchPattern = "Micser.Plugins.*.dll";

        public const string UserRegistryRoot = @"Software\Micser";

        public static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
            "Micser");

        public static class DriverUtility
        {
            public const string DeviceCount = "c";
            public const string Silent = "s";

            public static readonly string[] ParamNameChars = { "/" };

            public static class ReturnCodes
            {
                public const int InvalidParameter = -2;
                public const int RegistryAccessFailed = -10;
                public const int RequiresAdminAccess = -3;
                public const int SendControlSignalFailed = -11;
                public const int Success = 0;
                public const int UnknownError = -1;
            }
        }
    }
}