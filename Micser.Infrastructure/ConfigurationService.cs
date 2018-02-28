namespace Micser.Infrastructure
{
    public class ConfigurationService : IConfigurationService
    {
        public T GetSetting<T>(string key, T defaultValue)
        {
            throw new System.NotImplementedException();
        }

        public bool Save(string fileName = null)
        {
            throw new System.NotImplementedException();
        }

        public void SetSetting<T>(string key, T value)
        {
            throw new System.NotImplementedException();
        }
    }
}