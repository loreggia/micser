using System.Windows;
using System.Windows.Controls;
using Micser.Infrastructure.Themes;

namespace Micser.Infrastructure.Controls
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