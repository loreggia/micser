using System.Windows;
using System.Windows.Controls;
using Micser.Main.Themes;

namespace Micser.Main.Controls
{
    public class Widget : ContentControl
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(object), typeof(Widget), new PropertyMetadata(default(object)));

        static Widget()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Widget), new FrameworkPropertyMetadata(typeof(Widget)));
        }

        public Widget()
        {
            Resources.MergedDictionaries.Add(ResourceManager.SharedDictionary);
        }

        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }
    }
}