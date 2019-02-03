using Micser.Common.DataAccess;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Micser.App.Infrastructure
{
    public class SettingsService : ISettingsService
    {
        private const string SettingsTableName = "Settings";
        private readonly IUnitOfWorkFactory _database;

        private readonly IDictionary<string, object> _settings;

        public SettingsService(IUnitOfWorkFactory database)
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
            using (var uow = _database.Create())
            {
            }

            //            var settings = context.GetObject<IDictionary<string, object>>(SettingsTableName);
            //
            //            if (settings != null)
            //            {
            //                lock (_settings)
            //                {
            //                    _settings.Clear();
            //                    settings.ForEach(p => _settings.Add(p.Key, p.Value));
            //                }
            //            }
        }

        public void Save()
        {
            using (var uow = _database.Create())
            {
            }
            //            lock (_settings)
            //            {
            //                context.SetObject(_settings, SettingsTableName);
            //                context.Save();
            //            }
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