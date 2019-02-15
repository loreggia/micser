using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Menu
{
    public class MenuItemContainerTemplateSelector : ItemContainerTemplateSelector
    {
        public DataTemplate ItemTemplate { get; set; }
        public DataTemplate SeparatorTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, ItemsControl parentItemsControl)
        {
            if (item is TreeNode<MenuItemDescription> node && node.Item.IsSeparator)
            {
                return SeparatorTemplate;
            }

            return ItemTemplate ?? base.SelectTemplate(item, parentItemsControl);
        }
    }
}