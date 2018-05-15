using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Micser.Infrastructure
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IDictionary<string, object> _configuration;
        private readonly ILogger _logger;

        public ConfigurationService(ILogger logger)
        {
            _logger = logger;
            _configuration = new ConcurrentDictionary<string, object>();
        }

        public T GetSetting<T>(string key, T defaultValue = default(T), Func<JObject, T> deserializer = null)
        {
            if (_configuration.TryGetValue(key, out var result))
            {
                if (deserializer != null && result is JObject jObject)
                {
                    return deserializer(jObject);
                }

                return (T)result;
            }

            return defaultValue;
        }

        public IEnumerable<T> GetSettingEnumerable<T>(string key)
        {
            if (_configuration.TryGetValue(key, out var result))
            {
                if (result is JArray jArray)
                {
                    return jArray.Select(x => x.ToObject<T>());
                }

                return (IEnumerable<T>)result;
            }

            return null;
        }

        public bool Load(string fileName = null)
        {
            _configuration.Clear();

            fileName = fileName ?? GetDefaultFileName();

            if (!File.Exists(fileName))
            {
                return false;
            }

            try
            {
                var loadedConfiguration =
                    JsonConvert.DeserializeObject<IDictionary<string, object>>(File.ReadAllText(fileName));
                foreach (var key in loadedConfiguration.Keys)
                {
                    _configuration.Add(key, loadedConfiguration[key]);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return false;
            }
        }

        public bool Save(string fileName = null)
        {
            fileName = fileName ?? GetDefaultFileName();

            try
            {
                var directory = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(fileName, JsonConvert.SerializeObject(_configuration, Formatting.Indented));
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return false;
            }
        }

        public void SetSetting<T>(string key, T value)
        {
            if (_configuration.ContainsKey(key))
            {
                _configuration[key] = value;
            }
            else
            {
                _configuration.Add(key, value);
            }
        }

        protected virtual string GetDefaultFileName()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataFolder, "Micser", "defaultSettings.json");
        }
    }
}