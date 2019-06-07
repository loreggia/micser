using System;
using System.IO;

namespace Micser.Common
{
    /// <summary>
    /// Contains constants that are relevant for the whole framework.
    /// </summary>
    public static class Globals
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// The TCP port over which the API is talking.
        /// </summary>
        public const int ApiPort = 60666;

        /// <summary>
        /// The name of the virtual audio cable device. This has to be equal to the "DeviceName" string in Micser.Vac.Driver.inf
        /// </summary>
        public const string DeviceInterfaceName = "Micser Virtual Audio Cable";

        /// <summary>
        /// The maximum number of virtual audio cables.
        /// </summary>
        public const int MaxVacCount = 8;

        /// <summary>
        /// Search pattern for plugin DLLs.
        /// </summary>
        public const string PluginSearchPattern = "Micser.Plugins.*.dll";

        /// <summary>
        /// Registry root under HKCR to store driver settings.
        /// </summary>
        public const string UserRegistryRoot = @"Software\Micser";

        /// <summary>
        /// The framework's folder in the current user's AppData directory.
        /// </summary>
        public static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
            "Micser");

        public static class ApiResources
        {
            public const string Default = "<default>";
            public const string Engine = "Engine";
            public const string ModuleConnections = "moduleconnections";
            public const string Modules = "modules";
            public const string Status = "status";
        }

        /// <summary>
        /// Constants for managing the virtual audio cable driver using the Micser.DriverUtility application
        /// </summary>
        public static class DriverUtility
        {
            /// <summary>
            /// The file name of the driver utility program.
            /// </summary>
            public const string ProgramName = "Micser.DriverUtility.exe";

            /// <summary>
            /// The prefix(es) for argument names.
            /// </summary>
            public static readonly string[] ArgumentNameChars = { "/" };

            /// <summary>
            /// Contains argument names.
            /// </summary>
            public static class Arguments
            {
                /// <summary>
                /// Number of virtual audio cables.
                /// </summary>
                public const string DeviceCount = "c";

                /// <summary>
                /// Flag to suppress console output (used during installation).
                /// </summary>
                public const string Silent = "s";
            }

            /// <summary>
            /// Contains program return codes to identify errors.
            /// </summary>
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

        /// <summary>
        /// Contains registry value names.
        /// </summary>
        public static class RegistryValues
        {
            /// <summary>
            /// Stores the number of virtual audio cables.
            /// </summary>
            public const string VacCount = "DeviceCount";
        }

        /// <summary>
        /// Contains module/widget state keys.
        /// </summary>
        public static class StateKeys
        {
            /// <summary>
            /// Hardware device ID.
            /// </summary>
            public const string DeviceId = "DeviceId";
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}