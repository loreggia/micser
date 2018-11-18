using System;
using System.IO;
using System.Net;

namespace Micser.Infrastructure
{
    public static class Globals
    {
        public const int ApiPort = 60666;
        public const HttpStatusCode InternalErrorStatusCode = HttpStatusCode.InternalServerError;
        public const string PluginSearchPattern = "Micser.Plugins.*.dll";

        public static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
            "Micser");

        public static readonly string AppDbLocation = Path.Combine(AppDataFolder, "App.json");
        public static readonly string EngineDbLocation = Path.Combine(AppDataFolder, "Engine.json");

        public static class PrismRegions
        {
            public const string Main = "MainRegion";
            public const string Menu = "MenuRegion";
            public const string Status = "StatusRegion";
        }
    }
}