using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Settings
{
    /// <summary>
    /// Provides access to the currently stored setting values.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Event that is fired when a setting value changes.
        /// </summary>
        event SettingChangedEventHandler SettingChanged;

        /// <summary>
        /// Gets the value for the specified setting.
        /// </summary>
        T GetSetting<T>(string key);

        /// <summary>
        /// Gets a dictionary of all setting keys/values.
        /// </summary>
        IReadOnlyDictionary<string, object> GetSettings();

        /// <summary>
        /// Loads the settings from the database.
        /// </summary>
        Task LoadAsync();

        /// <summary>
        /// Sets a setting value and saves it in the database.
        /// </summary>
        void SetSetting(string key, object value);
    }
}