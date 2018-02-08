using System.Windows;
using System.Windows.Controls;

namespace DiagramDesigner
{
    public class Toolbox : ItemsControl
    {
        public static readonly DependencyProperty ItemSizeProperty = DependencyProperty.Register(
            nameof(ItemSize), typeof(Size), typeof(Toolbox), new PropertyMetadata(new Size(50, 50)));

        /// <summary>
        /// Defines the ItemHeight and ItemWidth properties of the WrapPanel used for this Toolbox.
        /// </summary>
        public Size ItemSize
        {
            get => (Size)GetValue(ItemSizeProperty);
            set => SetValue(ItemSizeProperty, value);
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ToolboxItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is ToolboxItem;
        }
    }
}