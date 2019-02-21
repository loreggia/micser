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
            try
            {
                var fileName = Path.Combine(StartupFolder, StartupShortcutFileName);
                return File.Exists(fileName);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        public static void SetStartupSetting(object value, ILogger logger)
        {
            value = value ?? false;

            if (value is bool isEnabled)
            {
                var fileName = Path.Combine(StartupFolder, StartupShortcutFileName);
                var fileExists = File.Exists(fileName);

                if (isEnabled && !fileExists)
                {
                    // create shortcut
                    try
                    {
                        var shell = new WshShell();
                        var shortcut = (IWshShortcut)shell.CreateShortcut(fileName);
                        shortcut.TargetPath = Assembly.GetExecutingAssembly().Location;
                        shortcut.WorkingDirectory = Path.GetDirectoryName(shortcut.TargetPath);
                        shortcut.Save();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Could not create the startup shortcut.");
                    }
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
                        logger.Error(ex, "Could not delete the startup shortcut.");
                    }
                }
            }
            else
            {
                logger.Warn($"Invalid value '{value}', expected a boolean.");
            }
        }
    }
}