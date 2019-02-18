namespace Micser.App.Infrastructure.Settings
{
    public enum SettingType
    {
        Boolean,
        String,
        Integer,
        Decimal,
    }

    public class SettingDefinition
    {
        public object DefaultValue { get; set; }
        public string Description { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public SettingType Type { get; set; }
    }
}