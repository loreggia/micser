using System;
using System.Windows;
using System.Windows.Controls;

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
            SetResourceReference(StyleProperty, typeof(View));

            Dispatcher.ShutdownStarted += OnDispatcherShutdownStarted;
            DataContextChanged += OnDataContextChanged;
        }

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public ViewModel ViewModel => DataContext as ViewModel;

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var commandBindings = ViewModel?.CommandBindings;
            if (commandBindings != null)
            {
                CommandBindings.AddRange(commandBindings);
            }
        }

        private void OnDispatcherShutdownStarted(object sender, EventArgs e)
        {
            ViewModel?.Dispose();
        }
    }
}