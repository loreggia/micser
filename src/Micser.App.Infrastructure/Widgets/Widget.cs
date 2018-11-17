using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Themes;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Micser.App.Infrastructure.Widgets
{
    [TemplatePart(Name = PartNameInputConnectors, Type = typeof(ItemsControl))]
    [TemplatePart(Name = PartNameOutputConnectors, Type = typeof(ItemsControl))]
    public class Widget : ContentControl, ISelectable
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(object), typeof(Widget), new PropertyMetadata(null));

        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
            nameof(HeaderTemplate), typeof(DataTemplate), typeof(Widget), new PropertyMetadata(null));

        public static readonly DependencyProperty InputConnectorsSourceProperty = DependencyProperty.Register(
            nameof(InputConnectorsSource), typeof(IEnumerable<ConnectorViewModel>), typeof(Widget), new PropertyMetadata(null));

        public static readonly DependencyProperty InputConnectorTemplateProperty = DependencyProperty.Register(
            nameof(InputConnectorTemplate), typeof(DataTemplate), typeof(Widget), new PropertyMetadata(null));

        public static readonly DependencyProperty IsDragConnectionOverProperty = DependencyProperty.Register(
            nameof(IsDragConnectionOver), typeof(bool), typeof(Widget), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(Widget), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty OutputConnectorsSourceProperty = DependencyProperty.Register(
            nameof(OutputConnectorsSource), typeof(IEnumerable<ConnectorViewModel>), typeof(Widget), new PropertyMetadata(null));

        public static readonly DependencyProperty OutputConnectorTemplateProperty = DependencyProperty.Register(
            nameof(OutputConnectorTemplate), typeof(DataTemplate), typeof(Widget), new PropertyMetadata(null));

        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            nameof(Position), typeof(Point), typeof(Widget), new PropertyMetadata(default(Point), OnPositionPropertyChanged));

        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            nameof(Size), typeof(Size), typeof(Widget), new PropertyMetadata(default(Size), OnSizePropertyChanged));

        internal const string PartNameInputConnectors = "PART_InputConnectors";

        internal const string PartNameOutputConnectors = "PART_OutputConnectors";

        private ItemsControl _inputConnectorsControl;

        private ItemsControl _outputConnectorsControl;

        static Widget()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Widget), new FrameworkPropertyMetadata(typeof(Widget)));
        }

        public Widget()
        {
            ResourceRegistry.RegisterResourcesFor(this);
            SetResourceReference(StyleProperty, typeof(Widget));

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

        public IEnumerable<Connector> InputConnectors => _inputConnectorsControl?.GetItemsChildren<Connector>();

        public IEnumerable<ConnectorViewModel> InputConnectorsSource
        {
            get => (IEnumerable<ConnectorViewModel>)GetValue(InputConnectorsSourceProperty);
            set => SetValue(InputConnectorsSourceProperty, value);
        }

        public DataTemplate InputConnectorTemplate
        {
            get => (DataTemplate)GetValue(InputConnectorTemplateProperty);
            set => SetValue(InputConnectorTemplateProperty, value);
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

        public IEnumerable<Connector> OutputConnectors => _outputConnectorsControl?.GetItemsChildren<Connector>();

        public IEnumerable<ConnectorViewModel> OutputConnectorsSource
        {
            get => (IEnumerable<ConnectorViewModel>)GetValue(OutputConnectorsSourceProperty);
            set => SetValue(OutputConnectorsSourceProperty, value);
        }

        public DataTemplate OutputConnectorTemplate
        {
            get => (DataTemplate)GetValue(OutputConnectorTemplateProperty);
            set => SetValue(OutputConnectorTemplateProperty, value);
        }

        public Point Position
        {
            get => (Point)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public Size Size
        {
            get => (Size)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _inputConnectorsControl = (ItemsControl)GetTemplateChild(PartNameInputConnectors);
            _outputConnectorsControl = (ItemsControl)GetTemplateChild(PartNameOutputConnectors);
        }

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
                    }
                    else
                    {
                        IsSelected = true;
                    }
                }
                else if (!IsSelected)
                {
                    panel.ClearSelection();
                    IsSelected = true;
                }
            }

            e.Handled = false;
        }

        private static void OnPositionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Point point && d is UIElement element)
            {
                Canvas.SetLeft(element, point.X);
                Canvas.SetTop(element, point.Y);
            }
        }

        private static void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Size size && d is FrameworkElement element)
            {
                element.Width = size.Width;
                element.Height = size.Height;
            }
        }

        private void OnDispatcherShutdownStarted(object sender, EventArgs e)
        {
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void OnWidgetLoaded(object sender, RoutedEventArgs e)
        {
            //ViewModel?.Initialize();
            //GetBindingExpression(PositionProperty)?.UpdateTarget();
        }
    }
}