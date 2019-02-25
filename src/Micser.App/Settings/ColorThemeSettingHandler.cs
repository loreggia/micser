using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Settings;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Micser.App.Settings
{
    public class ColorThemeSettingHandler : IListSettingHandler
    {
        private readonly ILogger _logger;
        private Dictionary<string, string> _themeFiles;
        private Dictionary<object, string> _themeList;

        public ColorThemeSettingHandler(ILogger logger)
        {
            _logger = logger;
        }

        public IDictionary<object, string> CreateList()
        {
            _themeList = new Dictionary<object, string>();
            _themeFiles = new Dictionary<string, string>();

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
                            _logger.Error($"Theme definition '{iniFilePath}' is invalid.");
                            continue;
                        }

                        var themeFilePath = Path.ChangeExtension(iniFilePath, ".xaml");
                        if (!File.Exists(themeFilePath))
                        {
                            _logger.Error($"Missing theme file for definition '{iniFilePath}'");
                            continue;
                        }

                        _themeList.Add(name, name);
                        _themeFiles.Add(name, themeFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                    }
                }
            }

            return _themeList;
        }

        public object OnLoadSetting(object value)
        {
            if (value is string name && _themeFiles.TryGetValue(name, out var fileName))
            {
                ApplyTheme(fileName);
            }

            return value;
        }

        public object OnSaveSetting(object value)
        {
            if (value is string name && _themeFiles.TryGetValue(name, out var fileName))
            {
                ApplyTheme(fileName);
            }

            return value;
        }

        private void ApplyTheme(string fileName)
        {
        }
    }
}