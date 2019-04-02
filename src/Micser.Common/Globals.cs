using System;
using System.IO;

namespace Micser.Common
{
    public static class Globals
    {
        public const int ApiPort = 60666;

        public const string PluginSearchPattern = "Micser.Plugins.*.dll";

        public static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
            "Micser");

        public static class DriverUtility
        {
            public const string DeviceId = "id";
            public const string InstallFlag = "i";
            public const string UninstallFlag = "u";
            public static readonly string[] ParamNameChars = { "/" };
        }
    }
}