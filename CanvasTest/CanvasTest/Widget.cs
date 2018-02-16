using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace CanvasTest
{
    public class Widget : ContentControl
    {
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
                                                        "Position",
                                                        typeof(Point),
                                                        typeof(Widget),
                                                        new PropertyMetadata(default(Point), OnPositionPropertyChanged));

        static Widget()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Widget), new FrameworkPropertyMetadata(typeof(Widget)));
        }

        public Point Position
        {
            get => (Point)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        private static void OnPositionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Point p && d is UIElement element)
            {
                Debug.WriteLine($"Widget.OnPositionPropertyChanged: {p}");
                Canvas.SetLeft(element, p.X);
                Canvas.SetTop(element, p.Y);
            }
        }
    }
}