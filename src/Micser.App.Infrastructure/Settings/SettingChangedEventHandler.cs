using System;

namespace Micser.App.Infrastructure.Settings
{
    public delegate void SettingChangedEventHandler(object sender, SettingChangedEventArgs e);

    public class SettingChangedEventArgs : EventArgs
    {
        public SettingChangedEventArgs(SettingDefinition setting, object oldValue, object newValue)
        {
            Setting = setting;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public object NewValue { get; }
        public object OldValue { get; }
        public SettingDefinition Setting { get; }
    }
}