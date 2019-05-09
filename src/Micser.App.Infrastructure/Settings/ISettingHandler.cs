using System.Collections.Generic;

namespace Micser.App.Infrastructure.Settings
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
        object OnLoadSetting(object value);

        object OnSaveSetting(object value);
    }
}