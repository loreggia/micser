using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Micser.Infrastructure.Controls;

namespace Micser.Infrastructure
{
    public class View : UserControl
    {
        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            nameof(IsBusy), typeof(bool), typeof(View), new PropertyMetadata(false, IsBusyPropertyChanged));

        private BusyAdorner _busyAdorner;

        public View()
        {
            Dispatcher.ShutdownStarted += OnDispatcherShutdownStarted;
        }

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public ViewModel ViewModel => DataContext as ViewModel;

        private static void IsBusyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (View)d;

            var layer = AdornerLayer.GetAdornerLayer(view);

            if (view.IsBusy)
            {
                layer.Add(view._busyAdorner ?? (view._busyAdorner = new BusyAdorner(view)));
            }
            else
            {
                var adorners = layer.GetAdorners(view);
                if (adorners != null)
                {
                    foreach (var adorner in adorners)
                    {
                        if (adorner is BusyAdorner)
                        {
                            layer.Remove(adorner);
                        }
                    }
                }
            }
        }

        private void OnDispatcherShutdownStarted(object sender, EventArgs e)
        {
            ViewModel?.Dispose();
        }
    }
}