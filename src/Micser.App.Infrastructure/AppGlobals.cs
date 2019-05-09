﻿namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Contains constants and global values.
    /// </summary>
    public static class AppGlobals
    {
        /// <summary>
        /// The internal key identifying the parameter during prism navigation.
        /// </summary>
        public const string NavigationParameterKey = "_NavigationParameter";

        /// <summary>
        /// The name of the directory where themes are stored.
        /// </summary>
        public const string ThemesDirectoryName = "Themes";

        /// <summary>
        /// Contains default menu item IDs.
        /// </summary>
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
            public const string ToolsVac = Tools + "Vac";
        }

        /// <summary>
        /// Contains constant keys that identify calculated properties on a module state.
        /// </summary>
        public static class ModuleStateKeys
        {
            public const string Height = "Height";
            public const string Left = "Left";
            public const string Top = "Top";
            public const string Width = "Width";
        }

        /// <summary>
        /// Contains constant names of the default prism regions.
        /// </summary>
        public static class PrismRegions
        {
            public const string Main = "MainRegion";
            public const string Menu = "MenuRegion";
            public const string Status = "StatusRegion";
            public const string TopToolBar = "TopToolBarRegion";
        }

        /// <summary>
        /// Contains constants identifying program startup arguments.
        /// </summary>
        public static class ProgramArguments
        {
            public const string Startup = "startup";
        }

        /// <summary>
        /// Contains constant keys identifying the default setting values.
        /// </summary>
        public static class SettingKeys
        {
            public const string ColorTheme = "ColorTheme";
            public const string MinimizeToTray = "MinimizeToTray";
            public const string ShellState = "ShellState";
            public const string Startup = "Startup";
            public const string StartupMinimized = "StartupMinimized";
        }

        /// <summary>
        /// Contains constant values used for identifying sections/keys in a theme .ini file.
        /// </summary>
        public static class ThemeIni
        {
            public const string GeneralSection = "General";
            public const string NameKey = "Name";
        }

        /// <summary>
        /// Contains constant keys identifying the default tool bars.
        /// </summary>
        public static class ToolBarIds
        {
            public const string Main = "MainToolBar";
        }

        /// <summary>
        /// Contains constants used in widget UI components.
        /// </summary>
        public static class Widgets
        {
            public const string LabelsSharedSizeGroup = "Labels";
        }
    }
}