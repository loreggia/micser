using System;

namespace Micser.App.Infrastructure.Settings
{
    public enum SettingStorageType
    {
        Internal,
        Custom
    }

    public enum SettingType
    {
        Boolean,
        String,
        Integer,
        Decimal
    }

    public class SettingDefinition
    {
        public SettingDefinition()
        {
            Type = SettingType.String;
            StorageType = SettingStorageType.Internal;
        }

        public object DefaultValue { get; set; }
        public string Description { get; set; }
        public Func<object> GetCustomSetting { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public Action<object> SetCustomSetting { get; set; }
        public SettingStorageType StorageType { get; set; }
        public SettingType Type { get; set; }
    }
}