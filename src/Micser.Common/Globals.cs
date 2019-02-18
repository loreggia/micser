using System;
using System.IO;
using System.Net;

namespace Micser.Common
{
    public static class Globals
    {
        public const int ApiPort = 60666;
        public const HttpStatusCode InternalErrorStatusCode = HttpStatusCode.InternalServerError;
        public const string PluginSearchPattern = "Micser.Plugins.*.dll";

        public static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
            "Micser");
    }
}