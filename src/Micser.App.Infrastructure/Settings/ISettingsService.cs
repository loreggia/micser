using System.Collections.Generic;

namespace Micser.App.Infrastructure.Settings
{
    public interface ISettingsService
    {
        T GetSetting<T>(string key);

        IReadOnlyDictionary<string, object> GetSettings();

        void Load();

        void SetSetting(string key, object value);
    }
}