namespace Micser.App.Infrastructure
{
    public interface ISettingsService
    {
        T GetSetting<T>(string key, T defaultValue = default(T));

        void SetSetting<T>(string key, T value);
    }
}