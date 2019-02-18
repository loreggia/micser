namespace Micser.App.Infrastructure.Settings
{
    public abstract class SettingDefinition
    {
        public SettingDefinition(string key, object defaultValue)
        {
            Key = key;
            DefaultValue = defaultValue;
        }

        public object DefaultValue { get; set; }
        public string Key { get; set; }
    }
}