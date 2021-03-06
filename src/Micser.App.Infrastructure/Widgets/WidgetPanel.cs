﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// A customized <see cref="Canvas"/> control managing widgets and their connections.
    /// </summary>
    public class WidgetPanel : Canvas
    {
        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="ConnectionsSource"/> property.
        /// </summary>
        public static readonly DependencyProperty ConnectionsSourceProperty = DependencyProperty.Register(
            nameof(ConnectionsSource), typeof(IEnumerable<ConnectionViewModel>), typeof(WidgetPanel),
            new PropertyMetadata(null, OnConnectionsSourcePropertyChanged));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="GridSize"/> property.
        /// </summary>
        public static readonly DependencyProperty GridSizeProperty = DependencyProperty.Register(
            nameof(GridSize), typeof(double), typeof(WidgetPanel), new PropertyMetadata(10d));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="IsGridVisible"/> property.
        /// </summary>
        public static readonly DependencyProperty IsGridVisibleProperty = DependencyProperty.Register(
            nameof(IsGridVisible), typeof(bool), typeof(WidgetPanel), new PropertyMetadata(false, OnIsGridVisiblePropertyChanged));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="WidgetFactory"/> property.
        /// </summary>
        public static readonly DependencyProperty WidgetFactoryProperty = DependencyProperty.Register(
            nameof(WidgetFactory), typeof(IWidgetFactory), typeof(WidgetPanel),
            new PropertyMetadata(null, OnWidgetFactoryPropertyChanged));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="WidgetsSource"/> property.
        /// </summary>
        public static readonly DependencyProperty WidgetsSourceProperty = DependencyProperty.Register(
            nameof(WidgetsSource), typeof(IEnumerable<WidgetViewModel>), typeof(WidgetPanel),
            new PropertyMetadata(null, OnWidgetsSourcePropertyChanged));

        private readonly ObservableCollection<Connection> _connections;

        private readonly SemaphoreSlim _loadSemaphore;
        private readonly ObservableCollection<Widget> _widgets;
        private bool _isLoaded;
        private Point? _rubberbandSelectionStartPoint;

        /// <inheritdoc />
        public WidgetPanel()
        {
            _loadSemaphore = new SemaphoreSlim(1, 1);

            _widgets = new ObservableCollection<Widget>();
            _widgets.CollectionChanged += Widgets_CollectionChanged;
            _connections = new ObservableCollection<Connection>();
            _connections.CollectionChanged += Connections_CollectionChanged;
            Loaded += OnLoaded;
        }

        /// <summary>
        /// Gets or sets the connections between the widgets.
        /// </summary>
        public IEnumerable<ConnectionViewModel> ConnectionsSource
        {
            get => (IEnumerable<ConnectionViewModel>)GetValue(ConnectionsSourceProperty);
            set => SetValue(ConnectionsSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the grid size of the snapping grid that is shown while moving a widget.
        /// </summary>
        public double GridSize
        {
            get => (double)GetValue(GridSizeProperty);
            set => SetValue(GridSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the background grid is visible.
        /// </summary>
        public bool IsGridVisible
        {
            get => (bool)GetValue(IsGridVisibleProperty);
            set => SetValue(IsGridVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="IWidgetFactory"/> instance that is used to create widgets based on the <see cref="WidgetsSource"/> collection.
        /// </summary>
        public IWidgetFactory WidgetFactory
        {
            get => (IWidgetFactory)GetValue(WidgetFactoryProperty);
            set => SetValue(WidgetFactoryProperty, value);
        }

        /// <summary>
        /// Gets the currently loaded widgets.
        /// </summary>
        public IEnumerable<Widget> Widgets => _widgets;

        /// <summary>
        /// Gets or sets a collection of <see cref="WidgetViewModel"/>s from which to create widgets using the <see cref="WidgetFactory"/>.
        /// </summary>
        public IEnumerable<WidgetViewModel> WidgetsSource
        {
            get => (IEnumerable<WidgetViewModel>)GetValue(WidgetsSourceProperty);
            set => SetValue(WidgetsSourceProperty, value);
        }

        /// <summary>
        /// Deselects all widgets.
        /// </summary>
        public void ClearSelection()
        {
            foreach (ISelectable selectable in Children)
            {
                selectable.IsSelected = false;
            }
        }

        /// <summary>
        /// Create a connection between an output and an input connector.
        /// </summary>
        public void CreateConnection(Connector sourceConnector, Connector targetConnector)
        {
            if (sourceConnector.DataContext is ConnectorViewModel sourceVm &&
                targetConnector.DataContext is ConnectorViewModel targetVm &&
                ConnectionsSource != null &&
                !ConnectionsSource.Any(c => c.Source == sourceVm && c.Target == targetVm))
            {
                var vm = new ConnectionViewModel
                {
                    Source = sourceVm,
                    Target = targetVm
                };

                if (ConnectionsSource is ICollection<ConnectionViewModel> collection)
                {
                    collection.Add(vm);
                }

                if (!(ConnectionsSource is INotifyCollectionChanged))
                {
                    AddConnection(vm);
                }
            }
        }

        /// <summary>
        /// Removes the specified connection.
        /// </summary>
        public void RemoveConnection(Connection connection)
        {
            if (connection == null || !(connection.DataContext is ConnectionViewModel vm))
            {
                return;
            }

            if (ConnectionsSource is ICollection<ConnectionViewModel> collection)
            {
                collection.Remove(vm);
            }

            if (!(ConnectionsSource is INotifyCollectionChanged))
            {
                _connections.Remove(connection);
            }
        }

        /// <summary>
        /// Removes the specified widget.
        /// </summary>
        public void RemoveWidget(Widget widget)
        {
            if (widget == null || !(widget.DataContext is WidgetViewModel vm))
            {
                return;
            }

            if (WidgetsSource is ICollection<WidgetViewModel> collection)
            {
                collection.Remove(vm);
            }

            if (!(WidgetsSource is INotifyCollectionChanged))
            {
                _widgets.Remove(widget);
            }
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size constraint)
        {
            var size = new Size();

            foreach (UIElement element in Children)
            {
                var left = GetLeft(element);
                var top = GetTop(element);
                left = double.IsNaN(left) ? 0 : left;
                top = double.IsNaN(top) ? 0 : top;

                if (element is FrameworkElement fe)
                {
                    var width = fe.Width;
                    var height = fe.Height;

                    fe.Width = double.IsNaN(width) ? fe.MinWidth : width;
                    fe.Height = double.IsNaN(height) ? fe.MinHeight : height;
                }

                //measure desired size for each child
                try
                {
                    element.Measure(constraint);
                }
                catch (InvalidOperationException)
                {
                }

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

        /// <inheritdoc />
        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            // Handle drag & drop from the widget tool box
            if (e.Data.GetData(typeof(WidgetDescription)) is WidgetDescription description)
            {
                if (WidgetFactory == null)
                {
                    throw new InvalidOperationException("No widget factory available.");
                }

                var vm = WidgetFactory.CreateViewModel(description.ViewModelType);
                vm.Name = description.Name?.ToString();
                vm.Position = e.GetPosition(this);

                if (WidgetsSource is ICollection<WidgetViewModel> collection)
                {
                    collection.Add(vm);
                }

                if (!(WidgetsSource is INotifyCollectionChanged))
                {
                    var widget = WidgetFactory.CreateWidget(vm);
                    _widgets.Add(widget);
                }

                SnapWidgetsToGrid();

                //e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Delete widgets on key press
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                var selected = _widgets.Where(w => w.IsSelected).ToArray();
                foreach (var widget in selected)
                {
                    var connections = _connections.Where(c => c.Source.ParentWidget == widget || c.Target.ParentWidget == widget).ToArray();
                    foreach (var connection in connections)
                    {
                        RemoveConnection(connection);
                    }

                    RemoveWidget(widget);
                }

                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            // Handle rubberband selection
            if (Equals(e.Source, this))
            {
                // in case that this click is the start for a
                // drag operation we cache the start point
                _rubberbandSelectionStartPoint = e.GetPosition(this);

                // if you click directly on the canvas all
                // selected items are deselected
                foreach (var widget in _widgets)
                {
                    widget.IsSelected = false;
                }

                foreach (var connection in _connections)
                {
                    connection.IsSelected = false;
                }

                Focus();
                e.Handled = true;
            }
        }

        /// <inheritdoc />
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
                // create rubber band adorner
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (adornerLayer != null)
                {
                    var adorner = new RubberbandAdorner(this, _rubberbandSelectionStartPoint);
                    adornerLayer.Add(adorner);
                }
            }

            //e.Handled = true;
        }

        private static void OnConnectionsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WidgetPanel)d;

            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= panel.ConnectionsSource_CollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += panel.ConnectionsSource_CollectionChanged;
            }

            if (panel._isLoaded)
            {
                panel.LoadConnectionsSource();
            }
        }

        private static void OnIsGridVisiblePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WidgetPanel)d;
            if (!(e.NewValue is bool b) || !b)
            {
                panel.SnapWidgetsToGrid();
            }
        }

        private static void OnWidgetFactoryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WidgetPanel)d;
            if (panel._isLoaded && e.NewValue != null && !panel._widgets.Any() && panel.WidgetsSource != null)
            {
                panel.LoadWidgetsSource();
            }
        }

        private static void OnWidgetsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WidgetPanel)d;

            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= panel.WidgetsSource_CollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += panel.WidgetsSource_CollectionChanged;
            }

            if (panel._isLoaded)
            {
                panel.LoadWidgetsSource();
            }
        }

        private async void AddConnection(ConnectionViewModel vm)
        {
            for (var i = 0; i < 10; i++)
            {
                if (_widgets.All(w => w.IsLoaded))
                {
                    break;
                }

                await Task.Delay(100);
            }

            var source = _widgets
                .SelectMany(w => w.OutputConnectors ?? new Connector[0])
                .FirstOrDefault(c => c.DataContext == vm.Source);
            var target = _widgets
                .SelectMany(w => w.InputConnectors ?? new Connector[0])
                .FirstOrDefault(c => c.DataContext == vm.Target);

            if (source != null && target != null && !_connections.Any(c => ReferenceEquals(c.Source, source) && ReferenceEquals(c.Target, target)))
            {
                var connection = new Connection(source, target) { DataContext = vm };
                _connections.Add(connection);
            }
            else
            {
                // todo log
            }
        }

        private void Connections_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ClearSelection();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Connection connection in e.NewItems)
                    {
                        Children.Add(connection);
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Connection connection in e.OldItems)
                    {
                        Children.Remove(connection);
                    }

                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (Connection connection in e.OldItems)
                    {
                        Children.Remove(connection);
                    }

                    foreach (Connection connection in e.NewItems)
                    {
                        Children.Add(connection);
                    }

                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    var connections = Children.OfType<Connection>().ToArray();
                    foreach (var connection in connections)
                    {
                        Children.Remove(connection);
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async void ConnectionsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            await _loadSemaphore.WaitAsync();
            var isLoaded = _isLoaded;
            _loadSemaphore.Release();

            if (!isLoaded)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (ConnectionViewModel item in e.NewItems)
                    {
                        AddConnection(item);
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (ConnectionViewModel item in e.OldItems)
                    {
                        var connection = _connections.FirstOrDefault(c => c.DataContext == item);
                        if (connection != null)
                        {
                            _connections.Remove(connection);
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (ConnectionViewModel item in e.OldItems)
                    {
                        var connection = _connections.FirstOrDefault(c => c.DataContext == item);
                        if (connection != null)
                        {
                            _connections.Remove(connection);
                        }
                    }

                    foreach (ConnectionViewModel item in e.NewItems)
                    {
                        AddConnection(item);
                    }

                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _connections.Clear();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void LoadConnectionsSource()
        {
            _connections.Clear();
            if (ConnectionsSource != null)
            {
                foreach (var vm in ConnectionsSource)
                {
                    AddConnection(vm);
                }
            }
        }

        private void LoadWidgetsSource()
        {
            _widgets.Clear();
            if (WidgetsSource != null && WidgetFactory != null)
            {
                foreach (var item in WidgetsSource)
                {
                    _widgets.Add(WidgetFactory.CreateWidget(item));
                }
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _loadSemaphore.WaitAsync();
                LoadWidgetsSource();
                for (var i = 0; i < 10; i++)
                {
                    await Task.Delay(100);
                    if (_widgets.All(w => w.IsLoaded))
                    {
                        LoadConnectionsSource();
                        break;
                    }
                }
            }
            finally
            {
                _isLoaded = true;
                _loadSemaphore.Release();
            }
        }

        private void SnapValueToGridSize(ref double value)
        {
            var snap = value % GridSize;

            if (snap <= GridSize / 2d)
            {
                snap *= -1;
            }
            else
            {
                snap = GridSize - snap;
            }

            value += snap;
        }

        private void SnapWidgetsToGrid()
        {
            foreach (var widget in Widgets)
            {
                // snap position
                var left = widget.Position.X;
                var top = widget.Position.Y;
                SnapValueToGridSize(ref left);
                SnapValueToGridSize(ref top);
                widget.Position = new Point(left, top);

                // snap size
                var width = widget.Size.Width;
                var height = widget.Size.Height;
                SnapValueToGridSize(ref width);
                SnapValueToGridSize(ref height);
                widget.Size = new Size(width, height);
            }
        }

        private void Widgets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ClearSelection();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Widget widget in e.NewItems)
                    {
                        Children.Add(widget);
                        widget.IsSelected = true;
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

        private async void WidgetsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            await _loadSemaphore.WaitAsync();
            var isLoaded = _isLoaded;
            _loadSemaphore.Release();

            if (!isLoaded)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (WidgetFactory != null)
                    {
                        foreach (WidgetViewModel item in e.NewItems)
                        {
                            _widgets.Add(WidgetFactory.CreateWidget(item));
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (WidgetViewModel item in e.OldItems)
                    {
                        var widget = _widgets.FirstOrDefault(w => w.DataContext == item);
                        if (widget != null)
                        {
                            _widgets.Remove(widget);
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (WidgetViewModel item in e.OldItems)
                    {
                        var widget = _widgets.FirstOrDefault(w => w.DataContext == item);
                        if (widget != null)
                        {
                            _widgets.Remove(widget);
                        }
                    }

                    if (WidgetFactory != null)
                    {
                        foreach (WidgetViewModel item in e.NewItems)
                        {
                            _widgets.Add(WidgetFactory.CreateWidget(item));
                        }
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
    }
}