using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Settings
{
    /// <summary>
    /// An <see cref="ItemsControl"/> that hosts a list of settings.
    /// </summary>
    public class SettingsPanel : ItemsControl
    {
        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new SettingContainer();
        }

        /// <summary>
        /// Always false.
        /// </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return false;
        }
    }
}