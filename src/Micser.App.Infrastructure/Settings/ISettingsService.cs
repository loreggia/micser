using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Settings
{
    public interface ISettingsService
    {
        event SettingChangedEventHandler SettingChanged;

        T GetSetting<T>(string key);

        IReadOnlyDictionary<string, object> GetSettings();

        Task LoadAsync();

        void SetSetting(string key, object value);
    }
}