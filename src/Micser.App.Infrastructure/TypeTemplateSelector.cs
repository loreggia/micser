using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure
{
    public class TypeTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null && container is FrameworkElement element)
            {
                if (element.TryFindResource(item.GetType()) is DataTemplate template)
                {
                    return template;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}