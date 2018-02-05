using System;
using System.Windows;
using System.Windows.Controls;
using Micser.Main.Themes;
using Micser.Main.ViewModels.Widgets;

namespace Micser.Main.Controls
{
    public class Widget : ContentControl
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(object), typeof(Widget), new PropertyMetadata(null));

        static Widget()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Widget), new FrameworkPropertyMetadata(typeof(Widget)));
        }

        public Widget()
        {
            Resources.MergedDictionaries.Add(ResourceManager.SharedDictionary);
            Dispatcher.ShutdownStarted += OnDispatcherShutdownStarted;
        }

        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public WidgetViewModel ViewModel => DataContext as WidgetViewModel;

        private void OnDispatcherShutdownStarted(object sender, EventArgs e)
        {
            ViewModel?.Dispose();
        }
    }
}