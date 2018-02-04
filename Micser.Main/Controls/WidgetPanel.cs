﻿using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Micser.Infrastructure.Extensions;
using Micser.Main.Themes;
using Micser.Main.ViewModels.Widgets;

namespace Micser.Main.Controls
{
    public class WidgetPanel : Canvas
    {
        public static readonly DependencyProperty IsWidgetLayoutChangingProperty = DependencyProperty.Register(
            nameof(IsWidgetLayoutChanging), typeof(bool), typeof(WidgetPanel), new PropertyMetadata(false));

        public static readonly DependencyProperty RasterSizeProperty = DependencyProperty.Register(
            nameof(RasterSize), typeof(double), typeof(WidgetPanel), new PropertyMetadata(25d));

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

        private static void OnWidgetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        private static void OnWidgetsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyCollectionChanged oldValue)
            {
                oldValue.CollectionChanged -= OnWidgetsCollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newValue)
            {
                newValue.CollectionChanged += OnWidgetsCollectionChanged;
            }

            if (e.NewValue is IEnumerable enumerable)
            {
                foreach (WidgetViewModel wvm in enumerable)
                {
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