using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Micser.Infrastructure.Themes;
using Micser.Infrastructure.ViewModels;

namespace Micser.Infrastructure.Controls
{
    public class Widget : ContentControl, ISelectable
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(object), typeof(Widget), new PropertyMetadata(null));

        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
            nameof(HeaderTemplate), typeof(DataTemplate), typeof(Widget), new PropertyMetadata(null));

        public static readonly DependencyProperty IsDragConnectionOverProperty = DependencyProperty.Register(
            nameof(IsDragConnectionOver), typeof(bool), typeof(Widget), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(Widget), new FrameworkPropertyMetadata(false));

        static Widget()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Widget), new FrameworkPropertyMetadata(typeof(Widget)));
        }

        public Widget()
        {
            ResourceRegistry.RegisterResourcesFor(this);

            Loaded += OnWidgetLoaded;
            Dispatcher.ShutdownStarted += OnDispatcherShutdownStarted;
        }

        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public DataTemplate HeaderTemplate
        {
            get => (DataTemplate)GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// While drag connection procedure is ongoing and the mouse moves over this item this value is true; if true the ConnectorDecorator is triggered to be visible, see template.
        /// </summary>
        public bool IsDragConnectionOver
        {
            get => (bool)GetValue(IsDragConnectionOverProperty);
            set => SetValue(IsDragConnectionOverProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public WidgetViewModel ViewModel => DataContext as WidgetViewModel;

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            // update selection
            if (VisualTreeHelper.GetParent(this) is WidgetPanel panel)
            {
                if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != ModifierKeys.None)
                {
                    if (IsSelected)
                    {
                        IsSelected = false;
                        panel.SelectedItems.Remove(this);
                    }
                    else
                    {
                        IsSelected = true;
                        panel.SelectedItems.Add(this);
                    }
                }
                else if (!IsSelected)
                {
                    foreach (var item in panel.SelectedItems)
                    {
                        item.IsSelected = false;
                    }

                    panel.SelectedItems.Clear();
                    IsSelected = true;
                    panel.SelectedItems.Add(this);
                }
            }

            e.Handled = false;
        }

        private void OnDispatcherShutdownStarted(object sender, EventArgs e)
        {
            ViewModel?.Dispose();
        }

        private void OnWidgetLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel?.Initialize();
        }
    }
}