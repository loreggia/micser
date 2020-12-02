using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Generic template selector that selects a template using the object's type as a template resource key.
    /// </summary>
    public class TypeTemplateSelector : DataTemplateSelector
    {
        /// <inheritdoc />
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