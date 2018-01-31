using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Micser.Infrastructure.Extensions;

namespace Micser.Main.Controls
{
    public class WidgetPanel : Canvas
    {
        public static readonly DependencyProperty IsWidgetLayoutChangingProperty = DependencyProperty.Register(
            nameof(IsWidgetLayoutChanging), typeof(bool), typeof(WidgetPanel), new PropertyMetadata(false));

        public static readonly DependencyProperty RasterSizeProperty = DependencyProperty.Register(
            nameof(RasterSize), typeof(double), typeof(WidgetPanel), new PropertyMetadata(25d));

        static WidgetPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WidgetPanel), new FrameworkPropertyMetadata(typeof(WidgetPanel)));
        }

        public WidgetPanel()
        {
            AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnWidgetLayoutChanged));
            AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnWidgetLayoutChanging));
        }

        public bool IsWidgetLayoutChanging
        {
            get => (bool)GetValue(IsWidgetLayoutChangingProperty);
            set => SetValue(IsWidgetLayoutChangingProperty, value);
        }

        public double RasterSize
        {
            get => (double)GetValue(RasterSizeProperty);
            set => SetValue(RasterSizeProperty, value);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            base.MeasureOverride(constraint);

            var desiredSize = new Size();
            foreach (UIElement child in Children)
            {
                child.EnsureCanvasTopLeft();

                var left = GetLeft(child);
                var top = GetTop(child);

                desiredSize = new Size(
                    Math.Max(desiredSize.Width, left + child.DesiredSize.Width),
                    Math.Max(desiredSize.Height, top + child.DesiredSize.Height));
            }
            return desiredSize;
        }

        private void OnWidgetLayoutChanged(object sender, DragCompletedEventArgs e)
        {
            if (e.Source is Widget widget)
            {
                SnapToGrid(widget);
                InvalidateMeasure();
            }

            IsWidgetLayoutChanging = false;
        }

        private void OnWidgetLayoutChanging(object sender, DragStartedEventArgs e)
        {
            if (e.Source is Widget)
            {
                IsWidgetLayoutChanging = true;
            }
        }

        private void SnapToGrid(FrameworkElement element)
        {
            if (element == null)
            {
                return;
            }

            // snap position
            var left = GetLeft(element);
            var top = GetTop(element);
            SnapToRasterSize(ref left);
            SnapToRasterSize(ref top);
            SetLeft(element, left);
            SetTop(element, top);

            // snap size
            var width = element.ActualWidth;
            var height = element.ActualHeight;
            SnapToRasterSize(ref width);
            SnapToRasterSize(ref height);
            element.Width = width;
            element.Height = height;
        }

        private void SnapToRasterSize(ref double value)
        {
            var snap = value % RasterSize;

            if (snap <= RasterSize / 2d)
            {
                snap *= -1;
            }
            else
            {
                snap = RasterSize - snap;
            }

            value += snap;
        }
    }
}