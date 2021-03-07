using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Micser.Common.Settings;
using Micser.DataAccess.Entities;
using Newtonsoft.Json;

namespace Micser.Settings
{
    /// <inheritdoc cref="ISettingsService"/>
    public class SettingsService<TDbContext> : ISettingsService, IDisposable
        where TDbContext : DbContext
    {
        //private readonly SettingsApiClient _apiClient;
        private readonly IDbContextFactory<TDbContext> _dbContextFactory;

        private readonly SemaphoreSlim _loadSemaphore;
        private readonly ILogger<SettingsService<TDbContext>> _logger;
        private readonly IEnumerable<SettingDefinition> _settingDefinitions;
        private readonly ISettingHandlerFactory _settingHandlerFactory;
        private readonly IDictionary<string, object?> _settings;
        private bool _isLoaded;

        public SettingsService(
            IDbContextFactory<TDbContext> dbContextFactory,
            ISettingHandlerFactory settingHandlerFactory,
            IEnumerable<SettingDefinition> settingDefinitions,
            //SettingsApiClient apiClient,
            ILogger<SettingsService<TDbContext>> logger)
        {
            _settings = new ConcurrentDictionary<string, object?>();
            _loadSemaphore = new SemaphoreSlim(1, 1);

            _dbContextFactory = dbContextFactory;
            _settingHandlerFactory = settingHandlerFactory;
            _settingDefinitions = settingDefinitions;
            //_apiClient = apiClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public event EventHandler<SettingChangedEventArgs>? SettingChanged;

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public object? GetSetting(string key)
        {
            if (!_settings.TryGetValue(key, out var value))
            {
                _logger.LogWarning($"Requested unregistered setting '{key}'.");
                return null;
            }

            return value;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?> GetSettings()
        {
            return new ReadOnlyDictionary<string, object?>(_settings);
        }

        /// <inheritdoc />
        public async Task LoadAsync(bool forceReload = false)
        {
            if (_isLoaded && !forceReload)
            {
                return;
            }

            await _loadSemaphore.WaitAsync().ConfigureAwait(false);

            if (_isLoaded && !forceReload)
            {
                _loadSemaphore.Release();
                return;
            }

            try
            {
                await using var dbContext = _dbContextFactory.CreateDbContext();

                foreach (var setting in _settingDefinitions)
                {
                    object? value = null;

                    if (setting.StorageType == SettingStorageType.Internal)
                    {
                        var settingValue = dbContext.Set<SettingValue>().FirstOrDefault(s => s.Key == setting.Key);

                        if (settingValue?.ValueJson != null && settingValue.ValueType != null)
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
                        //todo
                        //var settingValueResult = await _apiClient.GetSetting(setting.Key).ConfigureAwait(false);

                        //if (settingValueResult.IsSuccess)
                        //{
                        //    value = settingValueResult.Data != null ? settingValueResult.Data.Value : setting.DefaultValue;
                        //}
                        //else
                        //{
                        //    _logger.Warn($"Failed to get a setting from the setting API. Key: {setting.Key}, Result: {settingValueResult.Message}");
                        //}
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings.");
            }
            finally
            {
                _isLoaded = true;
                _loadSemaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetSettingAsync(string key, object? value)
        {
            await LoadAsync().ConfigureAwait(false);

            var setting = _settingDefinitions.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.InvariantCultureIgnoreCase));

            if (setting == null)
            {
                _logger.LogWarning($"Saving unregistered setting '{key}'.");
            }

            if (setting?.HandlerType != null)
            {
                var handler = _settingHandlerFactory.Create(setting.HandlerType);

                if (handler == null)
                {
                    _logger.LogWarning($"Setting handler type {setting.HandlerType} not registered.");
                }
                else
                {
                    value = await handler.SaveSettingAsync(value).ConfigureAwait(false);
                }
            }

            var oldValue = _settings.ContainsKey(key) ? _settings[key] : null;

            _settings[key] = value;

            OnSettingChanged(new SettingChangedEventArgs(setting, oldValue, value));

            if (setting == null || setting.StorageType == SettingStorageType.Internal)
            {
                await using var dbContext = _dbContextFactory.CreateDbContext();
                var dbSet = dbContext.Set<SettingValue>();
                var settingValue = await dbSet.FirstOrDefaultAsync(s => s.Key == key).ConfigureAwait(false)
                                   ?? new SettingValue { Key = key };

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
                    await dbSet.AddAsync(settingValue).ConfigureAwait(false);
                }

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            else if (setting.StorageType == SettingStorageType.Api)
            {
                //todo
                //var setSettingResult = await _apiClient.SetSetting(setting.Key, value).ConfigureAwait(false);

                //if (setSettingResult.IsSuccess)
                //{
                //    if (setSettingResult.Data != null)
                //    {
                //        value = setSettingResult.Data.Value;
                //    }
                //}
                //else
                //{
                //    _logger.Warn($"Could not save API setting. Key: {setting.Key}, Value: {value}, Result: {setSettingResult.Message}");
                //}
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
                _loadSemaphore.Dispose();
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