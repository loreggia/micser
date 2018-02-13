using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Micser.Infrastructure.Themes;

namespace Micser.Infrastructure.Controls
{
    public class WidgetPanel : Canvas
    {
        public static readonly DependencyProperty RasterSizeProperty = DependencyProperty.Register(
            nameof(RasterSize), typeof(double), typeof(WidgetPanel), new PropertyMetadata(25d));

        public static readonly DependencyProperty WidgetFactoryProperty = DependencyProperty.Register(
            nameof(WidgetFactory), typeof(IWidgetFactory), typeof(WidgetPanel), new PropertyMetadata(default(IWidgetFactory)));

        public static readonly DependencyProperty WidgetsProperty = DependencyProperty.Register(
            nameof(Widgets), typeof(IEnumerable<WidgetViewModel>), typeof(WidgetPanel), new PropertyMetadata(null, OnWidgetsPropertyChanged));

        // start point of the rubberband drag operation
        private Point? _rubberbandSelectionStartPoint;

        public WidgetPanel()
        {
            ResourceRegistry.RegisterResourcesFor(this);

            AllowDrop = true;
            SelectedItems = new List<ISelectable>();
        }

        public double RasterSize
        {
            get => (double)GetValue(RasterSizeProperty);
            set => SetValue(RasterSizeProperty, value);
        }

        public IList<ISelectable> SelectedItems { get; }

        public IWidgetFactory WidgetFactory
        {
            get => (IWidgetFactory)GetValue(WidgetFactoryProperty);
            set => SetValue(WidgetFactoryProperty, value);
        }

        public IEnumerable<WidgetViewModel> Widgets
        {
            get => (IEnumerable<WidgetViewModel>)GetValue(WidgetsProperty);
            set => SetValue(WidgetsProperty, value);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var size = new Size();

            foreach (UIElement element in Children)
            {
                var left = GetLeft(element);
                var top = GetTop(element);
                left = double.IsNaN(left) ? 0 : left;
                top = double.IsNaN(top) ? 0 : top;

                //measure desired size for each child
                element.Measure(constraint);

                var desiredSize = element.DesiredSize;
                if (!double.IsNaN(desiredSize.Width) && !double.IsNaN(desiredSize.Height))
                {
                    size.Width = Math.Max(size.Width, left + desiredSize.Width);
                    size.Height = Math.Max(size.Height, top + desiredSize.Height);
                }
            }

            // add margin
            size.Width += 10;
            size.Height += 10;
            return size;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetData(typeof(WidgetDescription)) is WidgetDescription description)
            {
                if (WidgetFactory == null)
                {
                    throw new InvalidOperationException("No widget factory available.");
                }

                var widget = WidgetFactory.CreateWidget(description);

                Children.Add(widget);

                var position = e.GetPosition(this);
                SetTop(widget, position.Y);
                SetLeft(widget, position.X);

                //update selection
                foreach (var item in SelectedItems)
                {
                    item.IsSelected = false;
                }

                SelectedItems.Clear();
                widget.IsSelected = true;
                SelectedItems.Add(widget);

                e.Handled = true;
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (Equals(e.Source, this))
            {
                // in case that this click is the start for a
                // drag operation we cache the start point
                _rubberbandSelectionStartPoint = e.GetPosition(this);

                // if you click directly on the canvas all
                // selected items are 'de-selected'
                foreach (var item in SelectedItems)
                {
                    item.IsSelected = false;
                }

                SelectedItems.Clear();

                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // if mouse button is not pressed we have no drag operation, ...
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _rubberbandSelectionStartPoint = null;
            }

            // ... but if mouse button is pressed and start
            // point value is set we do have one
            if (_rubberbandSelectionStartPoint.HasValue)
            {
                // create rubberband adorner
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (adornerLayer != null)
                {
                    var adorner = new RubberbandAdorner(this, _rubberbandSelectionStartPoint);
                    adornerLayer.Add(adorner);
                }
            }
            e.Handled = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            var widgets = SelectedItems.OfType<Widget>();
            foreach (var widget in widgets)
            {
                SnapToGrid(widget);
            }
        }

        private static void OnWidgetsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
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