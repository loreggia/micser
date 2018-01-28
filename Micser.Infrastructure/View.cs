﻿using System.Windows;
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
            Loaded += OnViewLoaded;
        }

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        protected virtual async void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            //if (DataContext is IViewModel vm)
            //{
            //    try
            //    {
            //        IsBusy = true;
            //        await vm.InitializeAsync();
            //    }
            //    finally
            //    {
            //        IsBusy = false;
            //    }
            //}
        }

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
    }
}