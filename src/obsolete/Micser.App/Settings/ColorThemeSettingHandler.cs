using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.Logging;
using Micser.App.Infrastructure;
using Micser.Common.Settings;

namespace Micser.App.Settings
{
    public class ColorThemeSettingHandler : IListSettingHandler
    {
        private readonly ILogger<ColorThemeSettingHandler> _logger;
        private readonly Dictionary<string, string> _themeFiles;
        private readonly Dictionary<object, string> _themeList;

        public ColorThemeSettingHandler(ILogger<ColorThemeSettingHandler> logger)
        {
            _logger = logger;
            _themeList = new Dictionary<object, string>();
            _themeFiles = new Dictionary<string, string>();
        }

        public IDictionary<object, string> CreateList()
        {
            LoadThemeFiles();
            return _themeList;
        }

        public async Task<object> LoadSettingAsync(object value)
        {
            await LoadThemeFilesAsync().ConfigureAwait(false);

            if (value is string name && _themeFiles.TryGetValue(name, out var fileName))
            {
                ApplyTheme(fileName);
            }

            return value;
        }

        public async Task<object> SaveSettingAsync(object value)
        {
            await LoadThemeFilesAsync().ConfigureAwait(false);

            if (value is string name && _themeFiles.TryGetValue(name, out var fileName))
            {
                ApplyTheme(fileName);
            }

            return value;
        }

        private static void ApplyTheme(string fileName)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => ApplyThemeInternal(fileName));
            }
            else
            {
                ApplyThemeInternal(fileName);
            }
        }

        private static void ApplyThemeInternal(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var dic = (ResourceDictionary)XamlReader.Load(fs);
                Application.Current.Resources.MergedDictionaries.Add(dic);
            }
        }

        private void LoadThemeFiles()
        {
            _themeList.Clear();
            _themeFiles.Clear();

            var themePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), AppGlobals.ThemesDirectoryName);
            if (Directory.Exists(themePath))
            {
                var iniFiles = Directory.GetFiles(themePath, "*.ini");

                foreach (var iniFilePath in iniFiles)
                {
                    try
                    {
                        var iniFile = new IniFile(iniFilePath);
                        var name = iniFile.GetValue(AppGlobals.ThemeIni.GeneralSection, AppGlobals.ThemeIni.NameKey);

                        if (string.IsNullOrEmpty(name))
                        {
                            _logger.LogError($"Theme definition '{iniFilePath}' is invalid.");
                            continue;
                        }

                        var themeFilePath = Path.ChangeExtension(iniFilePath, ".xaml");
                        if (!File.Exists(themeFilePath))
                        {
                            _logger.LogError($"Missing theme file for definition '{iniFilePath}'");
                            continue;
                        }

                        _themeList.Add(name, name);
                        _themeFiles.Add(name, themeFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to parse theme file '{iniFilePath}'.");
                    }
                }
            }
        }

        private Task LoadThemeFilesAsync()
        {
            return Task.Run(LoadThemeFiles);
        }
    }
}