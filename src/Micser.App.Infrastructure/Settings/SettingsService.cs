using Micser.App.Infrastructure.DataAccess.Models;
using Micser.App.Infrastructure.DataAccess.Repositories;
using Micser.Common.DataAccess;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Micser.App.Infrastructure.Settings
{
    public class SettingsService : ISettingsService
    {
        private readonly IUnitOfWorkFactory _database;
        private readonly ILogger _logger;
        private readonly ISettingsRegistry _registry;
        private readonly IDictionary<string, object> _settings;
        private bool _isLoaded;

        public SettingsService(IUnitOfWorkFactory database, ISettingsRegistry registry, ILogger logger)
        {
            _settings = new ConcurrentDictionary<string, object>();

            _database = database;
            _registry = registry;
            _logger = logger;
        }

        public T GetSetting<T>(string key)
        {
            EnsureLoaded();

            lock (_settings)
            {
                if (!_settings.ContainsKey(key))
                {
                    _logger.Warn($"Requested unregistered setting '{key}'.");
                    return default(T);
                }

                return (T)_settings[key];
            }
        }

        public void SetSetting<T>(string key, T value)
        {
            EnsureLoaded();

            lock (_settings)
            {
                if (!_settings.ContainsKey(key))
                {
                    _logger.Warn($"Saving unregistered setting '{key}'.");
                }

                _settings[key] = value;

                using (var uow = _database.Create())
                {
                    var settingRepo = uow.GetRepository<ISettingValueRepository>();
                    var settingValue = settingRepo.GetByKey(key) ?? new SettingValue();

                    var type = value?.GetType();

                    if (type == null)
                    {
                        settingValue.ValueJson = null;
                    }
                    else
                    {
                        settingValue.ValueType = type.AssemblyQualifiedName;
                        settingValue.ValueJson = JsonConvert.SerializeObject(value);
                    }

                    if (settingValue.Id <= 0)
                    {
                        settingRepo.Add(settingValue);
                    }

                    uow.Complete();
                }
            }
        }

        private void EnsureLoaded()
        {
            if (_isLoaded)
            {
                return;
            }

            lock (_settings)
            {
                if (_isLoaded)
                {
                    return;
                }

                Load();
            }
        }

        private void Load()
        {
            try
            {
                using (var uow = _database.Create())
                {
                    var settingRepo = uow.GetRepository<ISettingValueRepository>();

                    foreach (var setting in _registry.Items)
                    {
                        object value = null;
                        var settingValue = settingRepo.GetByKey(setting.Key);

                        if (settingValue?.ValueType != null)
                        {
                            var type = Type.GetType(settingValue.ValueType);

                            if (type != null)
                            {
                                value = JsonConvert.DeserializeObject(settingValue.ValueJson, type);
                            }
                        }

                        _settings[setting.Key] = value ?? setting.DefaultValue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            finally
            {
                _isLoaded = true;
            }
        }
    }
}