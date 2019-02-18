using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Settings
{
    public class SettingsPanel : ItemsControl
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ContentControl();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return false;
        }
    }
}