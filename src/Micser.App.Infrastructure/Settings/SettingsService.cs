using Micser.App.Infrastructure.DataAccess.Models;
using Micser.App.Infrastructure.DataAccess.Repositories;
using Micser.Common.DataAccess;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Settings
{
    /// <inheritdoc cref="ISettingsService"/>
    public class SettingsService : ISettingsService
    {
        private readonly IUnitOfWorkFactory _database;
        private readonly SemaphoreSlim _loadSemaphore;
        private readonly ILogger _logger;
        private readonly ISettingsRegistry _registry;
        private readonly IDictionary<string, object> _settings;
        private bool _isLoaded;

        public SettingsService(IUnitOfWorkFactory database, ISettingsRegistry registry, ILogger logger)
        {
            _settings = new ConcurrentDictionary<string, object>();
            _loadSemaphore = new SemaphoreSlim(1, 1);
            _database = database;
            _registry = registry;
            _logger = logger;
        }

        public event SettingChangedEventHandler SettingChanged;

        public T GetSetting<T>(string key)
        {
            if (!_settings.TryGetValue(key, out var value))
            {
                _logger.Warn($"Requested unregistered setting '{key}'.");
                return default(T);
            }

            if (value == null)
            {
                return default(T);
            }

            var valueType = value.GetType();
            var resultType = typeof(T);

            if (valueType == resultType || resultType.IsAssignableFrom(valueType))
            {
                return (T)value;
            }

            var valueConverter = TypeDescriptor.GetConverter(valueType);
            if (valueConverter.CanConvertTo(resultType))
            {
                return (T)valueConverter.ConvertTo(value, resultType);
            }

            var resultConverter = TypeDescriptor.GetConverter(resultType);
            if (resultConverter.CanConvertFrom(valueType))
            {
                return (T)resultConverter.ConvertFrom(value);
            }

            return default(T);
        }

        public IReadOnlyDictionary<string, object> GetSettings()
        {
            return new ReadOnlyDictionary<string, object>(_settings);
        }

        Task ISettingsService.LoadAsync()
        {
            return EnsureLoadedAsync();
        }

        public async void SetSetting(string key, object value)
        {
            await EnsureLoadedAsync();

            var setting = _registry.Items.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.InvariantCultureIgnoreCase));

            if (setting == null)
            {
                _logger.Warn($"Saving unregistered setting '{key}'.");
            }

            if (setting?.Handler != null)
            {
                value = setting.Handler.OnSaveSetting(value);
            }

            var oldValue = _settings.ContainsKey(key) ? _settings[key] : null;

            _settings[key] = value;

            OnSettingChanged(new SettingChangedEventArgs(setting, oldValue, value));

            if (setting?.StorageType == SettingStorageType.Custom)
            {
                return;
            }

            using (var uow = _database.Create())
            {
                var settingRepo = uow.GetRepository<ISettingValueRepository>();
                var settingValue = settingRepo.GetByKey(key) ?? new SettingValue { Key = key };

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

        protected virtual void OnSettingChanged(SettingChangedEventArgs e)
        {
            SettingChanged?.Invoke(this, e);
        }

        private async Task EnsureLoadedAsync()
        {
            if (_isLoaded)
            {
                return;
            }
            await _loadSemaphore.WaitAsync();
            if (_isLoaded)
            {
                _loadSemaphore.Release();
                return;
            }

            await LoadAsync();
            _loadSemaphore.Release();
        }

        private async Task LoadAsync()
        {
            try
            {
                using (var uow = _database.Create())
                {
                    var settingRepo = uow.GetRepository<ISettingValueRepository>();

                    foreach (var setting in _registry.Items)
                    {
                        object value = null;

                        if (setting.StorageType == SettingStorageType.Internal)
                        {
                            var settingValue = settingRepo.GetByKey(setting.Key);

                            if (settingValue?.ValueType != null)
                            {
                                var type = Type.GetType(settingValue.ValueType);

                                if (type != null)
                                {
                                    value = JsonConvert.DeserializeObject(settingValue.ValueJson, type);
                                }
                            }
                            else
                            {
                                value = setting.DefaultValue;
                            }
                        }

                        if (setting.Handler != null)
                        {
                            if (setting.Handler is IListSettingHandler listHandler)
                            {
                                setting.List = listHandler.CreateList();
                            }

                            value = await Task.Run(() => setting.Handler.OnLoadSetting(value ?? setting.DefaultValue));
                        }

                        _settings[setting.Key] = value;
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