using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable), typeof(WidgetPanel), new PropertyMetadata(null, OnItemsSourcePropertyChanged));

        public static readonly DependencyProperty RasterSizeProperty = DependencyProperty.Register(
            nameof(RasterSize), typeof(double), typeof(WidgetPanel), new PropertyMetadata(25d));

        public static readonly DependencyProperty WidgetFactoryProperty = DependencyProperty.Register(
            nameof(WidgetFactory), typeof(IWidgetFactory), typeof(WidgetPanel), new PropertyMetadata(null));

        private readonly ObservableCollection<Widget> _widgets;

        // start point of the rubberband drag operation
        private Point? _rubberbandSelectionStartPoint;

        public WidgetPanel()
        {
            _widgets = new ObservableCollection<Widget>();
            _widgets.CollectionChanged += WidgetsCollectionChanged;

            ResourceRegistry.RegisterResourcesFor(this);

            AllowDrop = true;
        }

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public double RasterSize
        {
            get => (double)GetValue(RasterSizeProperty);
            set => SetValue(RasterSizeProperty, value);
        }

        public IWidgetFactory WidgetFactory
        {
            get => (IWidgetFactory)GetValue(WidgetFactoryProperty);
            set => SetValue(WidgetFactoryProperty, value);
        }

        public IEnumerable<Widget> Widgets => _widgets;

        public void ClearSelection()
        {
            foreach (ISelectable selectable in Children)
            {
                selectable.IsSelected = false;
            }
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
                var position = e.GetPosition(this);
                SetTop(widget, position.Y);
                SetLeft(widget, position.X);
                _widgets.Add(widget);
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
                foreach (var widget in _widgets)
                {
                    widget.IsSelected = false;
                }

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

            // todo snapping
            //var widgets = SelectedItems.OfType<Widget>();
            //foreach (var widget in widgets)
            //{
            //    SnapToGrid(widget);
            //}
        }

        private static void OnItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WidgetPanel)d;

            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= panel.ItemsSourceCollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += panel.ItemsSourceCollectionChanged;
            }

            panel.ClearWidgets();

            if (e.NewValue is IEnumerable enumerable && panel.WidgetFactory != null)
            {
                foreach (var item in enumerable)
                {
                    panel.AddWidget(panel.WidgetFactory.CreateWidget(item));
                }
            }
        }

        private void AddWidget(Widget widget)
        {
            Children.Add(widget);

            //update selection
            foreach (var w in _widgets)
            {
                w.IsSelected = false;
            }

            widget.IsSelected = true;
        }

        private void ClearWidgets()
        {
            _widgets.Clear();
            Children.Clear();
        }

        private void ItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (WidgetFactory != null)
                    {
                        foreach (var item in e.NewItems)
                        {
                            AddWidget(WidgetFactory.CreateWidget(item));
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        RemoveWidget(_widgets.FirstOrDefault(w => w.DataContext == item));
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems)
                    {
                        RemoveWidget(_widgets.FirstOrDefault(w => w.DataContext == item));
                    }

                    foreach (var item in e.NewItems)
                    {
                        AddWidget(WidgetFactory.CreateWidget(item));
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    ClearWidgets();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RemoveWidget(Widget widget)
        {
            _widgets.Remove(widget);
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

        private void WidgetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Widget widget in e.NewItems)
                    {
                        Children.Add(widget);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Widget widget in e.OldItems)
                    {
                        Children.Remove(widget);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (Widget widget in e.OldItems)
                    {
                        Children.Remove(widget);
                    }

                    foreach (Widget widget in e.NewItems)
                    {
                        Children.Add(widget);
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Children.Clear();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}