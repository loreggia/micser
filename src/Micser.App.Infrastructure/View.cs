using System;
using System.Windows;
using System.Windows.Controls;
using Micser.App.Infrastructure.Themes;

namespace Micser.App.Infrastructure
{
    public class View : ContentControl
    {
        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            nameof(IsBusy), typeof(bool), typeof(View), new PropertyMetadata(false));

        static View()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(View), new FrameworkPropertyMetadata(typeof(View)));
        }

        public View()
        {
            ResourceRegistry.RegisterResourcesFor(this);
            SetResourceReference(StyleProperty, typeof(View));

            Dispatcher.ShutdownStarted += OnDispatcherShutdownStarted;
        }

        public bool IsBusy
        {
            get => (bool) GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public ViewModel ViewModel => DataContext as ViewModel;

        private void OnDispatcherShutdownStarted(object sender, EventArgs e)
        {
            ViewModel?.Dispose();
        }
    }
}