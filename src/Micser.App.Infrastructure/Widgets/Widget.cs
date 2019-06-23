using Micser.App.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Base view class for widgets.
    /// </summary>
    [TemplatePart(Name = PartNameInputConnectors, Type = typeof(ItemsControl))]
    [TemplatePart(Name = PartNameOutputConnectors, Type = typeof(ItemsControl))]
    public class Widget : ContentControl, ISelectable
    {
        /// <summary>
        /// The name of the input connectors template part.
        /// </summary>
        public const string PartNameInputConnectors = "PART_InputConnectors";

        /// <summary>
        /// The name of the output connectors template part.
        /// </summary>
        public const string PartNameOutputConnectors = "PART_OutputConnectors";

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="DeleteCommand"/> property.
        /// </summary>
        public static readonly DependencyProperty DeleteCommandProperty = DependencyProperty.Register(
            nameof(DeleteCommand), typeof(ICommand), typeof(Widget), new PropertyMetadata(null));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="Header"/> property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(object), typeof(Widget), new PropertyMetadata(null));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="HeaderTemplate"/> property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
            nameof(HeaderTemplate), typeof(DataTemplate), typeof(Widget), new PropertyMetadata(null));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="InputConnectorsSource"/> property.
        /// </summary>
        public static readonly DependencyProperty InputConnectorsSourceProperty = DependencyProperty.Register(
            nameof(InputConnectorsSource), typeof(IEnumerable<ConnectorViewModel>), typeof(Widget), new PropertyMetadata(null));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="InputConnectorTemplate"/> property.
        /// </summary>
        public static readonly DependencyProperty InputConnectorTemplateProperty = DependencyProperty.Register(
            nameof(InputConnectorTemplate), typeof(DataTemplate), typeof(Widget), new PropertyMetadata(null));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="IsDragConnectionOver"/> property.
        /// </summary>
        public static readonly DependencyProperty IsDragConnectionOverProperty = DependencyProperty.Register(
            nameof(IsDragConnectionOver), typeof(bool), typeof(Widget), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(Widget), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="OutputConnectorsSource"/> property.
        /// </summary>
        public static readonly DependencyProperty OutputConnectorsSourceProperty = DependencyProperty.Register(
            nameof(OutputConnectorsSource), typeof(IEnumerable<ConnectorViewModel>), typeof(Widget), new PropertyMetadata(null));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="OutputConnectorTemplate"/> property.
        /// </summary>
        public static readonly DependencyProperty OutputConnectorTemplateProperty = DependencyProperty.Register(
            nameof(OutputConnectorTemplate), typeof(DataTemplate), typeof(Widget), new PropertyMetadata(null));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="Position"/> property.
        /// </summary>
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            nameof(Position), typeof(Point), typeof(Widget), new PropertyMetadata(default(Point), OnPositionPropertyChanged));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="Size"/> property.
        /// </summary>
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            nameof(Size), typeof(Size), typeof(Widget), new PropertyMetadata(default(Size), OnSizePropertyChanged));

        private ItemsControl _inputConnectorsControl;

        private ItemsControl _outputConnectorsControl;

        /// <inheritdoc />
        public Widget()
        {
            SetResourceReference(StyleProperty, typeof(Widget));

            Loaded += OnWidgetLoaded;
            Dispatcher.ShutdownStarted += OnDispatcherShutdownStarted;
        }

        /// <summary>
        /// Gets or sets a command to execute when the delete context action is called.
        /// </summary>
        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the content that is shown in the widget's header using the <see cref="HeaderTemplate"/>.
        /// </summary>
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the template to display the <see cref="Header"/> content.
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get => (DataTemplate)GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// Gets the widget's input connector controls.
        /// </summary>
        public IEnumerable<Connector> InputConnectors => _inputConnectorsControl?.GetItemsChildren<Connector>();

        /// <summary>
        /// Gets or sets an enumerable of <see cref="ConnectionViewModel"/> from which to create this widget's input connectors.
        /// </summary>
        public IEnumerable<ConnectorViewModel> InputConnectorsSource
        {
            get => (IEnumerable<ConnectorViewModel>)GetValue(InputConnectorsSourceProperty);
            set => SetValue(InputConnectorsSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the template to apply for each input connector when using <see cref="InputConnectorsSource"/>.
        /// </summary>
        public DataTemplate InputConnectorTemplate
        {
            get => (DataTemplate)GetValue(InputConnectorTemplateProperty);
            set => SetValue(InputConnectorTemplateProperty, value);
        }

        /// <summary>
        /// While drag connection procedure is ongoing and the mouse moves over this item this value is true;
        /// if true the ConnectorDecorator is triggered to be visible, see template.
        /// </summary>
        public bool IsDragConnectionOver
        {
            get => (bool)GetValue(IsDragConnectionOverProperty);
            set => SetValue(IsDragConnectionOverProperty, value);
        }

        /// <summary>
        /// Gets or sets a value whether this widget is currently selected.
        /// </summary>
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        /// <summary>
        /// Gets this widget's output connector controls.
        /// </summary>
        public IEnumerable<Connector> OutputConnectors => _outputConnectorsControl?.GetItemsChildren<Connector>();

        /// <summary>
        /// Gets or sets an enumerable of <see cref="ConnectionViewModel"/> from which to create this widget's output connectors.
        /// </summary>
        public IEnumerable<ConnectorViewModel> OutputConnectorsSource
        {
            get => (IEnumerable<ConnectorViewModel>)GetValue(OutputConnectorsSourceProperty);
            set => SetValue(OutputConnectorsSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the template to apply for each output connector when using <see cref="OutputConnectorsSource"/>.
        /// </summary>
        public DataTemplate OutputConnectorTemplate
        {
            get => (DataTemplate)GetValue(OutputConnectorTemplateProperty);
            set => SetValue(OutputConnectorTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the widget's position (top left).
        /// </summary>
        public Point Position
        {
            get => (Point)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the widget's size (width, height).
        /// </summary>
        public Size Size
        {
            get => (Size)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _inputConnectorsControl = (ItemsControl)GetTemplateChild(PartNameInputConnectors);
            _outputConnectorsControl = (ItemsControl)GetTemplateChild(PartNameOutputConnectors);
        }

        /// <inheritdoc />
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