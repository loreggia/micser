using System.Collections;
using System.Windows;

namespace Micser.App.Infrastructure.Controls
{
    public class ToolBarTrayHelper
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.RegisterAttached(
            "ItemsSource", typeof(IEnumerable), typeof(ToolBarTrayHelper), new PropertyMetadata(null, OnItemsSourcePropertyChanged));

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.RegisterAttached(
            "ItemTemplate", typeof(DataTemplate), typeof(ToolBarTrayHelper), new PropertyMetadata(null, OnItemTemplateChanged));

        public static IEnumerable GetItemsSource(DependencyObject element)
        {
            return (IEnumerable)element.GetValue(ItemsSourceProperty);
        }

        public static DataTemplate GetItemTemplate(DependencyObject element)
        {
            return (DataTemplate)element.GetValue(ItemTemplateProperty);
        }

        public static void SetItemsSource(DependencyObject element, IEnumerable value)
        {
            element.SetValue(ItemsSourceProperty, value);
        }

        public static void SetItemTemplate(DependencyObject element, DataTemplate value)
        {
            element.SetValue(ItemTemplateProperty, value);
        }

        private static void OnItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
    }
}