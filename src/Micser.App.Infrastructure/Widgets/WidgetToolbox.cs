using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Widgets
{
    public class WidgetToolbox : ItemsControl
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new WidgetToolboxItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is WidgetToolboxItem;
        }
    }
}