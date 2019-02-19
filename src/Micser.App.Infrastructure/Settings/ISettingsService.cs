namespace Micser.App.Infrastructure.Settings
{
    public interface ISettingsService
    {
        T GetSetting<T>(string key);

        void Load();

        void SetSetting<T>(string key, T value);
    }
}