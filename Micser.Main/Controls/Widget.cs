using System.Windows;
using System.Windows.Controls;
using Micser.Main.Themes;

namespace Micser.Main.Controls
{
    public class Widget : Control
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content), typeof(object), typeof(Widget), new PropertyMetadata(null));

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(object), typeof(Widget), new PropertyMetadata(null));

        static Widget()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Widget), new FrameworkPropertyMetadata(typeof(Widget)));
        }

        public Widget()
        {
            Resources.MergedDictionaries.Add(ResourceManager.SharedDictionary);
        }

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }
    }
}