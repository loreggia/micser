using Micser.Common.DataAccess;
using Micser.Common.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Micser.App.Infrastructure
{
    public class SettingsService : ISettingsService
    {
        private const string SettingsTableName = "Settings";
        private readonly IDatabase _database;

        private readonly IDictionary<string, object> _settings;

        public SettingsService(IDatabase database)
        {
            _settings = new ConcurrentDictionary<string, object>();

            _database = database;
        }

        public T GetSetting<T>(string key, T defaultValue = default(T))
        {
            Load();

            lock (_settings)
            {
                if (_settings.ContainsKey(key))
                {
                    return _settings[key] is T ? (T)_settings[key] : defaultValue;
                }
            }

            return defaultValue;
        }

        public void Load()
        {
            var context = _database.GetContext();
            var settings = context.GetObject<IDictionary<string, object>>(SettingsTableName);

            if (settings != null)
            {
                lock (_settings)
                {
                    _settings.Clear();
                    settings.ForEach(p => _settings.Add(p.Key, p.Value));
                }
            }
        }

        public void Save()
        {
            var context = _database.GetContext();
            lock (_settings)
            {
                context.SetObject(_settings, SettingsTableName);
                context.Save();
            }
        }

        public void SetSetting<T>(string key, T value)
        {
            Load();

            lock (_settings)
            {
                if (_settings.ContainsKey(key))
                {
                    _settings[key] = value;
                }
                else
                {
                    _settings.Add(key, value);
                }
            }

            Save();
        }
    }
}