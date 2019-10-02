using Micser.App.Resources;
using Micser.Common.Settings;
using NLog;
using System;
using System.IO;
using System.Threading.Tasks;
using File = System.IO.File;

namespace Micser.App.Settings
{
    public class StartupSettingHandler : ISettingHandler
    {
        private static readonly string StartupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        private static readonly string StartupShortcutFileName = Strings.ApplicationTitle + ".lnk";
        private readonly ILogger _logger;

        public StartupSettingHandler(ILogger logger)
        {
            _logger = logger;
        }

        public Task<object> LoadSettingAsync(object value)
        {
            object result = false;
            try
            {
                var fileName = Path.Combine(StartupFolder, StartupShortcutFileName);
                result = File.Exists(fileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return Task.FromResult(result);
        }

        public async Task<object> SaveSettingAsync(object value)
        {
            value = value ?? false;

            if (value is bool isEnabled)
            {
                await Task.Run(() => SaveSetting(isEnabled)).ConfigureAwait(false);
            }
            else
            {
                _logger.Warn($"Invalid value '{value}', expected a boolean.");
            }

            return value;
        }

        private void SaveSetting(bool isEnabled)
        {
            var fileName = Path.Combine(StartupFolder, StartupShortcutFileName);
            var fileExists = File.Exists(fileName);

            if (isEnabled && !fileExists)
            {
                // create shortcut
                try
                {
                    //todo
                    //var shell = new WshShell();
                    //var shortcut = (IWshShortcut)shell.CreateShortcut(fileName);
                    //shortcut.TargetPath = Assembly.GetExecutingAssembly().Location;
                    //shortcut.WorkingDirectory = Path.GetDirectoryName(shortcut.TargetPath);
                    //shortcut.Arguments = "-" + AppGlobals.ProgramArguments.Startup;
                    //shortcut.Save();
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
    }
}