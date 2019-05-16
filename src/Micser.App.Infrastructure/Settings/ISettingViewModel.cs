namespace Micser.App.Infrastructure.Settings
{
    /// <summary>
    /// Common ancestor for the different setting view models.
    /// </summary>
    public interface ISettingViewModel
    {
        /// <summary>
        /// Gets the setting definition as registered during module initialization.
        /// </summary>
        SettingDefinition Definition { get; }

        /// <summary>
        /// Gets or sets a value whether the setting is changeable.
        /// </summary>
        bool IsEnabled { get; set; }
    }
}