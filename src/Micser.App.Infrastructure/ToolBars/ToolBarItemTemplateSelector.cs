using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.ToolBars
{
    public class ToolBarItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ButtonTemplate { get; set; }
        public DataTemplate LabelTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ToolBarButton)
            {
                return ButtonTemplate;
            }
            else if (item is ToolBarLabel)
            {
                return LabelTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}