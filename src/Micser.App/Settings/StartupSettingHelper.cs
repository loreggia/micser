using IWshRuntimeLibrary;
using Micser.App.Properties;
using NLog;
using System;
using System.IO;
using System.Reflection;
using File = System.IO.File;

namespace Micser.App.Settings
{
    public static class StartupSettingHelper
    {
        private static readonly string StartupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        private static readonly string StartupShortcutFileName = Resources.ApplicationTitle + ".lnk";

        public static object GetStartupSetting(ILogger logger)
        {
            var fileName = Path.Combine(StartupFolder, StartupShortcutFileName);
            return File.Exists(fileName);
        }

        public static void SetStartupSetting(object value, ILogger logger)
        {
            if (value is bool isEnabled)
            {
                var fileName = Path.Combine(StartupFolder, StartupShortcutFileName);
                var fileExists = File.Exists(fileName);

                if (isEnabled && !fileExists)
                {
                    // create shortcut
                    var shell = new WshShell();
                    var shortcut = (IWshShortcut)shell.CreateShortcut(fileName);
                    shortcut.TargetPath = Assembly.GetExecutingAssembly().Location;
                    shortcut.Save();
                }
                else if (!isEnabled && fileExists)
                {
                    // delete shortcut
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
    }
}