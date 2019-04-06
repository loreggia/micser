namespace Micser.App.Infrastructure
{
    public static class AppGlobals
    {
        public const string NavigationParameterKey = "_NavigationParameter";
        public const string ThemesDirectoryName = "Themes";

        public static class MenuItemIds
        {
            public const string File = "File";
            public const string FileExit = File + "Exit";
            public const string FileExport = File + "Export";
            public const string FileImport = File + "Import";
            public const string Help = "Help";
            public const string HelpAbout = Help + "About";
            public const string Tools = "Tools";
            public const string ToolsRefresh = Tools + "Refresh";
            public const string ToolsRestart = Tools + "Restart";
            public const string ToolsSettings = Tools + "Settings";
            public const string ToolsStart = Tools + "Start";
            public const string ToolsStop = Tools + "Stop";
        }

        public static class ModuleStateKeys
        {
            public const string Height = "Height";
            public const string Left = "Left";
            public const string Name = "Name";
            public const string Top = "Top";
            public const string Width = "Width";
        }

        public static class PrismRegions
        {
            public const string Main = "MainRegion";
            public const string Menu = "MenuRegion";
            public const string Status = "StatusRegion";
            public const string TopToolBar = "TopToolBarRegion";
        }

        public static class ProgramArguments
        {
            public const string Startup = "startup";
        }

        public static class SettingKeys
        {
            public const string ColorTheme = "ColorTheme";
            public const string MinimizeToTray = "MinimizeToTray";
            public const string ShellState = "ShellState";
            public const string Startup = "Startup";
            public const string StartupMinimized = "StartupMinimized";
        }

        public static class ThemeIni
        {
            public const string GeneralSection = "General";
            public const string NameKey = "Name";
        }

        public static class ToolBarIds
        {
            public const string Main = "MainToolBar";
        }

        public static class Widgets
        {
            public const string LabelsSharedSizeGroup = "Labels";
        }
    }
}