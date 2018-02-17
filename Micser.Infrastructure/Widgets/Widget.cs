using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Micser.Infrastructure.Themes;

namespace Micser.Infrastructure.Widgets
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

        internal const string PartNameInputConnectors = "PART_InputConnectors";
        internal const string PartNameOutputConnectors = "PART_OutputConnectors";
        private ObservableCollection<Connector> _inputConnectors;
        private ItemsControl _inputConnectorsControl;
        private ObservableCollection<Connector> _outputConnectors;
        private ItemsControl _outputConnectorsControl;

        static Widget()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Widget), new FrameworkPropertyMetadata(typeof(Widget)));
        }

        public Widget()
        {
            _inputConnectors = new ObservableCollection<Connector>();
            _outputConnectors = new ObservableCollection<Connector>();

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

        public IEnumerable<Connector> InputConnectors => _inputConnectorsControl.Items.SourceCollection.Cast<Connector>();

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

        public IEnumerable<Connector> OutputConnectors => _inputConnectorsControl.Items.SourceCollection.Cast<Connector>();

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

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _inputConnectorsControl = (ItemsControl)GetTemplateChild(PartNameInputConnectors);
            _outputConnectorsControl = (ItemsControl)GetTemplateChild(PartNameInputConnectors);
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