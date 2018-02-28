namespace Micser.Infrastructure
{
    public interface IConfigurationService
    {
        T GetSetting<T>(string key, T defaultValue = default(T));

        bool Save(string fileName = null);

        void SetSetting<T>(string key, T value);
    }
}