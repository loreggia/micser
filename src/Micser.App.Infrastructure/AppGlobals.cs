namespace Micser.App.Infrastructure
{
    public static class AppGlobals
    {
        public const string NavigationParameterKey = "_NavigationParameter";

        public static class MenuItemIds
        {
            public const string File = "File";
            public const string FileExit = File + "Exit";
            public const string FileLoad = "FileLoad";
            public const string FileSave = "FileSave";
            public const string Help = "Help";
            public const string HelpAbout = Help + "About";
            public const string Tools = "Tools";
            public const string ToolsRefresh = "ToolsRefresh";
            public const string ToolsSettings = Tools + "Settings";
        }

        public static class PrismRegions
        {
            public const string Main = "MainRegion";
            public const string Menu = "MenuRegion";
            public const string Status = "StatusRegion";
            public const string TopToolBar = "TopToolBarRegion";
        }

        public static class ToolBarIds
        {
            public const string Main = "MainToolBar";
        }
    }
}