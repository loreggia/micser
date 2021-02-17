using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.Common.Settings
{
    /// <summary>
    /// Provides access to the currently stored setting values.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Event that is fired when a setting value changes.
        /// </summary>
        event EventHandler<SettingChangedEventArgs> SettingChanged;

        /// <summary>
        /// Gets the value for the specified setting.
        /// </summary>
        object? GetSetting(string key);

        /// <summary>
        /// Gets a dictionary of all setting keys/values.
        /// </summary>
        IReadOnlyDictionary<string, object?> GetSettings();

        /// <summary>
        /// Loads the settings from the database.
        /// </summary>
        Task LoadAsync(bool forceReload = false);

        /// <summary>
        /// Sets a setting value and saves it in the database.
        /// </summary>
        /// <returns>True if the setting was changed, otherwise false.</returns>
        Task<bool> SetSettingAsync(string key, object? value);
    }
}