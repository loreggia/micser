using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Menu
{
    public class MenuItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ItemTemplate { get; set; }
        public DataTemplate SeparatorTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TreeNode<MenuItemDescription> node)
            {
                return node.Item.IsSeparator ? SeparatorTemplate : ItemTemplate;
            }

            return ItemTemplate;
        }
    }
}