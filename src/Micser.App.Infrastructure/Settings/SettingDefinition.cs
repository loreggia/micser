using System;
using System.Collections.Generic;

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
        Decimal,
        List,
        Object
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
        public ISettingHandler Handler { get; set; }
        public Func<ISettingsService, bool> IsEnabled { get; set; }
        public bool IsHidden { get; set; }
        public string Key { get; set; }
        public IDictionary<object, string> List { get; set; }
        public string Name { get; set; }
        public SettingStorageType StorageType { get; set; }
        public SettingType Type { get; set; }
    }
}