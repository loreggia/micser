using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Micser.Infrastructure.Extensions;
using Micser.Main.Themes;
using Micser.Main.ViewModels.Widgets;
using Micser.Main.Views.Widgets;

namespace Micser.Main.Controls
{
    public class WidgetPanel : Canvas
    {
        public static readonly DependencyProperty IsWidgetLayoutChangingProperty = DependencyProperty.Register(
            nameof(IsWidgetLayoutChanging), typeof(bool), typeof(WidgetPanel), new PropertyMetadata(false));

        public static readonly DependencyProperty RasterSizeProperty = DependencyProperty.Register(
            nameof(RasterSize), typeof(double), typeof(WidgetPanel), new PropertyMetadata(25d));

        public static readonly DependencyProperty WidgetFactoryProperty = DependencyProperty.Register(
            nameof(WidgetFactory), typeof(IWidgetFactory), typeof(WidgetPanel), new PropertyMetadata(null, OnWidgetFactoryPropertyChanged));

        public static readonly DependencyProperty WidgetsProperty = DependencyProperty.Register(
            nameof(Widgets), typeof(IEnumerable), typeof(WidgetPanel), new PropertyMetadata(null, OnWidgetsPropertyChanged));

        static WidgetPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WidgetPanel), new FrameworkPropertyMetadata(typeof(WidgetPanel)));
        }

        public WidgetPanel()
        {
            Resources.MergedDictionaries.Add(ResourceManager.SharedDictionary);

            AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnWidgetLayoutChanged));
            AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnWidgetLayoutChanging));

            Unloaded += OnUnloaded;
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

        public IWidgetFactory WidgetFactory
        {
            get => (IWidgetFactory)GetValue(WidgetFactoryProperty);
            set => SetValue(WidgetFactoryProperty, value);
        }

        public IEnumerable Widgets
        {
            get => (IEnumerable)GetValue(WidgetsProperty);
            set => SetValue(WidgetsProperty, value);
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

        private static void OnWidgetFactoryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WidgetPanel)d;
            panel.CreateWidgets();
        }

        private static void OnWidgetsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WidgetPanel)d;

            if (e.OldValue is INotifyCollectionChanged oldValue)
            {
                oldValue.CollectionChanged -= panel.OnWidgetsCollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newValue)
            {
                newValue.CollectionChanged += panel.OnWidgetsCollectionChanged;
            }

            panel.CreateWidgets();
        }

        private void AddWidget(WidgetViewModel vm)
        {
            if (vm == null || WidgetFactory == null)
            {
                return;
            }

            if (!Children.OfType<Widget>().Any(w => w.DataContext == vm))
            {
                Children.Add(WidgetFactory.CreateWidget(vm));
                vm.OnNavigatedTo(null);
            }
        }

        private void CreateWidgets()
        {
            if (WidgetFactory == null || Widgets == null)
            {
                return;
            }

            foreach (WidgetViewModel wvm in Widgets)
            {
                AddWidget(wvm);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (Widgets != null)
            {
                foreach (WidgetViewModel vm in Widgets)
                {
                    vm.OnNavigatedFrom(null);
                }
            }
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

        private void OnWidgetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (WidgetViewModel item in e.NewItems)
                {
                    AddWidget(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (WidgetViewModel item in e.OldItems)
                {
                    RemoveWidget(item);
                }
            }
        }

        private void RemoveWidget(WidgetViewModel vm)
        {
            if (vm == null)
            {
                return;
            }

            var widget = Children.OfType<Widget>().FirstOrDefault(w => w.DataContext == vm);
            if (widget != null)
            {
                vm.OnNavigatedFrom(null);
                Children.Remove(widget);
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