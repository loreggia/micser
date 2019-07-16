using Micser.Common.Api;
using Micser.Common.DataAccess;
using Micser.Common.DataAccess.Models;
using Micser.Common.DataAccess.Repositories;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Micser.Common.Settings
{
    /// <inheritdoc cref="ISettingsService"/>
    public class SettingsService : ISettingsService, IDisposable
    {
        private readonly SettingsApiClient _apiClient;
        private readonly IUnitOfWorkFactory _database;
        private readonly SemaphoreSlim _loadSemaphore;
        private readonly ILogger _logger;
        private readonly ISettingsRegistry _registry;
        private readonly ISettingHandlerFactory _settingHandlerFactory;
        private readonly IDictionary<string, object> _settings;
        private bool _isLoaded;

        /// <inheritdoc />
        public SettingsService(
            IUnitOfWorkFactory database,
            ISettingHandlerFactory settingHandlerFactory,
            ISettingsRegistry registry,
            SettingsApiClient apiClient,
            ILogger logger)
        {
            _settings = new ConcurrentDictionary<string, object>();
            _loadSemaphore = new SemaphoreSlim(1, 1);

            _database = database;
            _settingHandlerFactory = settingHandlerFactory;
            _registry = registry;
            _apiClient = apiClient;
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

        /// <inheritdoc />
        public async Task LoadAsync(bool forceReload = false)
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
                        else if (setting.StorageType == SettingStorageType.Api)
                        {
                            var settingValueResult = await _apiClient.GetSetting(setting.Key).ConfigureAwait(false);

                            if (settingValueResult.IsSuccess)
                            {
                                value = settingValueResult.Data != null ? settingValueResult.Data.Value : setting.DefaultValue;
                            }
                            else
                            {
                                _logger.Warn($"Failed to get a setting from the setting API. Key: {setting.Key}, Result: {settingValueResult.Message}");
                            }
                        }

                        if (setting.HandlerType != null)
                        {
                            var handler = _settingHandlerFactory.Create(setting.HandlerType);

                            if (handler is IListSettingHandler listHandler)
                            {
                                setting.List = listHandler.CreateList();
                            }

                            if (handler != null)
                            {
                                value = await handler.LoadSettingAsync(value ?? setting.DefaultValue).ConfigureAwait(false);
                            }
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
                _loadSemaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetSettingAsync(string key, object value)
        {
            await LoadAsync().ConfigureAwait(false);

            var setting = _registry.Items.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.InvariantCultureIgnoreCase));

            if (setting == null)
            {
                _logger.Warn($"Saving unregistered setting '{key}'.");
            }

            if (setting?.HandlerType != null)
            {
                var handler = _settingHandlerFactory.Create(setting.HandlerType);
                value = await handler.SaveSettingAsync(value).ConfigureAwait(false);
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
            else if (setting.StorageType == SettingStorageType.Api)
            {
                var setSettingResult = await _apiClient.SetSetting(setting.Key, value).ConfigureAwait(false);

                if (setSettingResult.IsSuccess)
                {
                    if (setSettingResult.Data != null)
                    {
                        value = setSettingResult.Data.Value;
                    }
                }
                else
                {
                    _logger.Warn($"Could not save API setting. Key: {setting.Key}, Value: {value}, Result: {setSettingResult.Message}");
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
    }
}