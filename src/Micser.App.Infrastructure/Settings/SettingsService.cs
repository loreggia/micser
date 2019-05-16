using Micser.App.Infrastructure.DataAccess.Models;
using Micser.App.Infrastructure.DataAccess.Repositories;
using Micser.Common.DataAccess;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Settings
{
    /// <inheritdoc cref="ISettingsService"/>
    public class SettingsService : ISettingsService, IDisposable
    {
        private readonly IUnitOfWorkFactory _database;
        private readonly SemaphoreSlim _loadSemaphore;
        private readonly ILogger _logger;
        private readonly ISettingsRegistry _registry;
        private readonly IDictionary<string, object> _settings;
        private bool _isLoaded;

        /// <inheritdoc />
        public SettingsService(IUnitOfWorkFactory database, ISettingsRegistry registry, ILogger logger)
        {
            _settings = new ConcurrentDictionary<string, object>();
            _loadSemaphore = new SemaphoreSlim(1, 1);
            _database = database;
            _registry = registry;
            _logger = logger;
        }

        /// <inheritdoc />
        public event SettingChangedEventHandler SettingChanged;

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public object GetSetting(string key)
        {
            if (!_settings.TryGetValue(key, out var value))
            {
                _logger.Warn($"Requested unregistered setting '{key}'.");
                return null;
            }

            return value;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> GetSettings()
        {
            return new ReadOnlyDictionary<string, object>(_settings);
        }

        Task ISettingsService.LoadAsync()
        {
            return Task.Run(() => EnsureLoaded(true));
        }

        /// <inheritdoc />
        public bool SetSetting(string key, object value)
        {
            EnsureLoaded();

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

            if (setting == null || setting.StorageType == SettingStorageType.Internal)
            {
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

            return !Equals(oldValue, value);
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _loadSemaphore?.Dispose();
            }
        }

        /// <summary>
        /// Raises the <see cref="SettingChanged"/> event.
        /// </summary>
        protected virtual void OnSettingChanged(SettingChangedEventArgs e)
        {
            SettingChanged?.Invoke(this, e);
        }

        private void EnsureLoaded(bool forceReload = false)
        {
            if (_isLoaded && !forceReload)
            {
                return;
            }
            _loadSemaphore.Wait();
            if (_isLoaded && !forceReload)
            {
                _loadSemaphore.Release();
                return;
            }

            Load();
            _loadSemaphore.Release();
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

                            value = setting.Handler.OnLoadSetting(value ?? setting.DefaultValue);
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