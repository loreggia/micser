using System.Windows;
using System.Windows.Controls;

namespace Micser.Main.Controls
{
    public class WidgetLink : Control
    {
        public static readonly DependencyProperty InputProperty = DependencyProperty.Register(
            "Input", typeof(Widget), typeof(WidgetLink), new PropertyMetadata(default(Widget)));

        public static readonly DependencyProperty OutputProperty = DependencyProperty.Register(
            "Output", typeof(Widget), typeof(WidgetLink), new PropertyMetadata(default(Widget)));

        static WidgetLink()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WidgetLink), new FrameworkPropertyMetadata(typeof(WidgetLink)));
        }

        public Widget Input
        {
            get { return (Widget)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        public Widget Output
        {
            get { return (Widget)GetValue(OutputProperty); }
            set { SetValue(OutputProperty, value); }
        }
    }
}