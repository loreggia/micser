using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.Common.Settings
{
    /// <summary>
    /// Provides methods to process loading and saving of a setting value that allows a list of specific values.
    /// </summary>
    public interface IListSettingHandler : ISettingHandler
    {
        /// <summary>
        /// Gets a list of possible setting values and their corresponding name that is shown in the settings view.
        /// </summary>
        IDictionary<object, string> CreateList();
    }

    /// <summary>
    /// Provides methods to process loading and saving of a setting value.
    /// </summary>
    public interface ISettingHandler
    {
        /// <summary>
        /// Handler that is executed when a setting is loaded.
        /// </summary>
        /// <param name="value">The internally stored setting value (if the corresponding <see cref="SettingDefinition.StorageType"/> is set to <see cref="SettingStorageType.Internal"/>).</param>
        /// <returns>The loaded setting value.</returns>
        Task<object> LoadSettingAsync(object value);

        /// <summary>
        /// Handler that is executed when a setting is saved.
        /// </summary>
        /// <param name="value">The value to save.</param>
        /// <returns>The actually saved value.</returns>
        Task<object> SaveSettingAsync(object value);
    }
}