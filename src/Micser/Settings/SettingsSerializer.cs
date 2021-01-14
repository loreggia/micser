using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Micser.Common.Settings;
using Newtonsoft.Json;

namespace Micser.Settings
{
    /// <summary>
    /// Exports/imports all application settings to or from a JSON file.
    /// </summary>
    public class SettingsSerializer
    {
        private readonly ILogger<SettingsSerializer> _logger;
        private readonly ISettingsService _settingsService;

        /// <inheritdoc />
        public SettingsSerializer(ILogger<SettingsSerializer> logger, ISettingsService settingsService)
        {
            _logger = logger;
            _settingsService = settingsService;
        }

        /// <summary>
        /// Exports the settings to the specified JSON file.
        /// </summary>
        public bool Export(string fileName)
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
                _logger.LogError(ex, $"Export to '{fileName}' failed.");
                return false;
            }
        }

        /// <summary>
        /// Imports the settings from the specified JSON file.
        /// </summary>
        public bool Import(string fileName)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    _logger.LogError($"Cannot load settings. File '{fileName}' not found.");
                    return false;
                }

                using (var fs = new StreamReader(fileName))
                using (var jsonReader = new JsonTextReader(fs))
                {
                    var serializer = JsonSerializer.CreateDefault();
                    var settings = serializer.Deserialize<Dictionary<string, object>>(jsonReader);

                    if (settings != null)
                    {
                        foreach (var setting in settings)
                        {
                            // TODO await
                            _settingsService.SetSettingAsync(setting.Key, setting.Value);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Import from '{fileName}' failed.");
                return false;
            }
        }
    }
}