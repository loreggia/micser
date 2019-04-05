using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;

namespace Micser.App.Infrastructure.Settings
{
    public class SettingsExporter
    {
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;

        public SettingsExporter(ILogger logger, ISettingsService settingsService)
        {
            _logger = logger;
            _settingsService = settingsService;
        }

        public bool Load(string fileName)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    _logger.Error($"Cannot load settings. File '{fileName}' not found.");
                    return false;
                }

                using (var fs = new StreamReader(fileName))
                using (var jsonReader = new JsonTextReader(fs))
                {
                    var serializer = JsonSerializer.CreateDefault();
                    var settings = serializer.Deserialize<Dictionary<string, object>>(jsonReader);

                    foreach (var setting in settings)
                    {
                        _settingsService.SetSetting(setting.Key, setting.Value);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return false;
            }
        }

        public bool Save(string fileName)
        {
            try
            {
                var dict = _settingsService.GetSettings();

                using (var fs = new StreamWriter(fileName))
                using (var jsonWriter = new JsonTextWriter(fs))
                {
                    var serializer = JsonSerializer.CreateDefault();
                    serializer.Serialize(jsonWriter, dict);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return false;
            }
        }
    }
}