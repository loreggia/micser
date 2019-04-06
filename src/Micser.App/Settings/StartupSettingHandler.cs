using IWshRuntimeLibrary;
using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Settings;
using Micser.App.Properties;
using NLog;
using System;
using System.IO;
using System.Reflection;
using File = System.IO.File;

namespace Micser.App.Settings
{
    public class StartupSettingHandler : ISettingHandler
    {
        private static readonly string StartupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        private static readonly string StartupShortcutFileName = Resources.ApplicationTitle + ".lnk";
        private readonly ILogger _logger;

        public StartupSettingHandler(ILogger logger)
        {
            _logger = logger;
        }

        public object OnLoadSetting(object value)
        {
            try
            {
                var fileName = Path.Combine(StartupFolder, StartupShortcutFileName);
                return File.Exists(fileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return false;
            }
        }

        public object OnSaveSetting(object value)
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
                        shortcut.Arguments = "-" + AppGlobals.ProgramArguments.Startup;
                        shortcut.Save();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Could not create the startup shortcut.");
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
                        _logger.Error(ex, "Could not delete the startup shortcut.");
                    }
                }
            }
            else
            {
                _logger.Warn($"Invalid value '{value}', expected a boolean.");
            }

            return value;
        }
    }
}