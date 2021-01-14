using System;

namespace Micser.Common.Settings
{
    /// <summary>
    /// Delegate for a setting changed event.
    /// </summary>
    public delegate void SettingChangedEventHandler(object sender, SettingChangedEventArgs e);

    /// <summary>
    /// Event arguments containing data that represents a changed setting.
    /// </summary>
    public class SettingChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates an instance of the <see cref="SettingChangedEventArgs"/> class.
        /// </summary>
        /// <param name="setting">The setting that was changed.</param>
        /// <param name="oldValue">The old setting value.</param>
        /// <param name="newValue">The new setting value.</param>
        public SettingChangedEventArgs(SettingDefinition? setting, object? oldValue, object? newValue)
        {
            Setting = setting;
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Gets the new setting value.
        /// </summary>
        public object? NewValue { get; }

        /// <summary>
        /// Gets the old setting value.
        /// </summary>
        public object? OldValue { get; }

        /// <summary>
        /// Gets the setting that was changed.
        /// </summary>
        public SettingDefinition? Setting { get; }
    }
}