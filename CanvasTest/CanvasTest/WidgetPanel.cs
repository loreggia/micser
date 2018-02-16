using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CanvasTest
{
    public class WidgetPanel : Canvas
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource", typeof(IEnumerable<WidgetViewModel>), typeof(WidgetPanel), new PropertyMetadata(null, OnItemsSourcePropertyChanged));

        private readonly ObservableCollection<Widget> _widgets;

        public WidgetPanel()
        {
            _widgets = new ObservableCollection<Widget>();
            _widgets.CollectionChanged += Widgets_CollectionChanged;
        }

        public IEnumerable<WidgetViewModel> ItemsSource
        {
            get => (IEnumerable<WidgetViewModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WidgetPanel)d;

            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= panel.ItemsSource_CollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += panel.ItemsSource_CollectionChanged;
            }

            panel._widgets.Clear();

            if (e.NewValue is IEnumerable enumerable)
            {
                foreach (WidgetViewModel vm in enumerable)
                {
                    panel._widgets.Add(new DerivedWidget { DataContext = vm });
                }
            }
        }

        private void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    //if (WidgetFactory != null)
                    //{
                    //    foreach (var item in e.NewItems)
                    //    {
                    //        _widgets.Add(WidgetFactory.CreateWidget(item));
                    //    }
                    //}
                    foreach (var vm in e.NewItems)
                    {
                        _widgets.Add(new DerivedWidget { DataContext = vm });
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        var widget = _widgets.FirstOrDefault(w => w.DataContext == item);
                        if (widget != null)
                        {
                            _widgets.Remove(widget);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems)
                    {
                        var widget = _widgets.FirstOrDefault(w => w.DataContext == item);
                        if (widget != null)
                        {
                            _widgets.Remove(widget);
                        }
                    }

                    //if (WidgetFactory != null)
                    //{
                    //    foreach (var item in e.NewItems)
                    //    {
                    //        _widgets.Add(WidgetFactory.CreateWidget(item));
                    //    }
                    //}
                    foreach (WidgetViewModel vm in e.NewItems)
                    {
                        _widgets.Add(new DerivedWidget { DataContext = vm });
                    }

                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _widgets.Clear();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Widgets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Widget widget in e.NewItems)
                    {
                        //var position = widget.Position;
                        Children.Add(widget);
                        //widget.Position = position;
                        //widget.IsSelected = true;
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
                        //var position = widget.Position;
                        Children.Add(widget);
                        //widget.Position = position;
                        //Dispatcher.BeginInvoke(new Func<Point>(() => widget.Position = position));
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