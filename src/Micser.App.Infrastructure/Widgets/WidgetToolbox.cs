using Micser.App.Infrastructure.Themes;
using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Widgets
{
    public class WidgetToolbox : ItemsControl
    {
        public WidgetToolbox()
        {
            ResourceRegistry.RegisterResourcesFor(this);
        }

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