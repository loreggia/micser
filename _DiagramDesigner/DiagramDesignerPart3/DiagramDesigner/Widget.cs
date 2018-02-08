using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DiagramDesigner.Controls;

namespace DiagramDesigner
{
    [TemplatePart(Name = "PART_DragThumb", Type = typeof(DragThumb))]
    [TemplatePart(Name = "PART_ResizeDecorator", Type = typeof(Control))]
    [TemplatePart(Name = "PART_ConnectorDecorator", Type = typeof(Control))]
    [TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
    public class Widget : ContentControl, ISelectable
    {
        /// <summary>
        /// Can be used to replace the default template for the ConnectorDecorator.
        /// </summary>
        public static readonly DependencyProperty ConnectorDecoratorTemplateProperty = DependencyProperty.RegisterAttached("ConnectorDecoratorTemplate", typeof(ControlTemplate), typeof(Widget));

        /// <summary>
        /// Can be used to replace the default template for the DragThumb.
        /// </summary>
        public static readonly DependencyProperty DragThumbTemplateProperty = DependencyProperty.RegisterAttached("DragThumbTemplate", typeof(ControlTemplate), typeof(Widget));

        public static readonly DependencyProperty IsDragConnectionOverProperty = DependencyProperty.Register(
            nameof(IsDragConnectionOver), typeof(bool), typeof(Widget), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(Widget), new FrameworkPropertyMetadata(false));

        static Widget()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Widget), new FrameworkPropertyMetadata(typeof(Widget)));
        }

        public Widget()
        {
            Loaded += OnWidgetLoaded;
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

        public static ControlTemplate GetConnectorDecoratorTemplate(UIElement element)
        {
            return (ControlTemplate)element.GetValue(ConnectorDecoratorTemplateProperty);
        }

        public static ControlTemplate GetDragThumbTemplate(UIElement element)
        {
            return (ControlTemplate)element.GetValue(DragThumbTemplateProperty);
        }

        public static void SetConnectorDecoratorTemplate(UIElement element, ControlTemplate value)
        {
            element.SetValue(ConnectorDecoratorTemplateProperty, value);
        }

        public static void SetDragThumbTemplate(UIElement element, ControlTemplate value)
        {
            element.SetValue(DragThumbTemplateProperty, value);
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
                        panel.SelectedItems.Remove(this);
                    }
                    else
                    {
                        IsSelected = true;
                        panel.SelectedItems.Add(this);
                    }
                }
                else if (!IsSelected)
                {
                    foreach (var item in panel.SelectedItems)
                    {
                        item.IsSelected = false;
                    }

                    panel.SelectedItems.Clear();
                    IsSelected = true;
                    panel.SelectedItems.Add(this);
                }
            }

            e.Handled = false;
        }

        private void OnWidgetLoaded(object sender, RoutedEventArgs e)
        {
            // if DragThumbTemplate and ConnectorDecoratorTemplate properties of this class
            // are set these templates are applied;
            // Note: this method is only executed when the Loaded event is fired, so
            // setting DragThumbTemplate or ConnectorDecoratorTemplate properties after
            // will have no effect.
            if (Template?.FindName("PART_ContentPresenter", this) is ContentPresenter contentPresenter)
            {
                if (VisualTreeHelper.GetChild(contentPresenter, 0) is UIElement contentVisual)
                {
                    var thumb = Template.FindName("PART_DragThumb", this) as DragThumb;
                    var connectorDecorator = Template.FindName("PART_ConnectorDecorator", this) as Control;

                    if (thumb != null)
                    {
                        if (GetDragThumbTemplate(contentVisual) is ControlTemplate template)
                        {
                            thumb.Template = template;
                        }
                    }

                    if (connectorDecorator != null)
                    {
                        if (GetConnectorDecoratorTemplate(contentVisual) is ControlTemplate template)
                        {
                            connectorDecorator.Template = template;
                        }
                    }
                }
            }
        }
    }
}