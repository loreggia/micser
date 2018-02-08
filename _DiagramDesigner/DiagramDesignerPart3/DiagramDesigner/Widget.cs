using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DiagramDesigner.Controls;

namespace DiagramDesigner
{
    //These attributes identify the types of the named parts that are used for templating
    [TemplatePart(Name = "PART_DragThumb", Type = typeof(DragThumb))]
    [TemplatePart(Name = "PART_ResizeDecorator", Type = typeof(Control))]
    [TemplatePart(Name = "PART_ConnectorDecorator", Type = typeof(Control))]
    [TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
    public class Widget : ContentControl, ISelectable
    {
        #region IsSelected Property

        public static readonly DependencyProperty IsSelectedProperty =
          DependencyProperty.Register("IsSelected",
                                       typeof(bool),
                                       typeof(Widget),
                                       new FrameworkPropertyMetadata(false));

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        #endregion IsSelected Property

        #region DragThumbTemplate Property

        // can be used to replace the default template for the DragThumb
        public static readonly DependencyProperty DragThumbTemplateProperty =
            DependencyProperty.RegisterAttached("DragThumbTemplate", typeof(ControlTemplate), typeof(Widget));

        public static ControlTemplate GetDragThumbTemplate(UIElement element)
        {
            return (ControlTemplate)element.GetValue(DragThumbTemplateProperty);
        }

        public static void SetDragThumbTemplate(UIElement element, ControlTemplate value)
        {
            element.SetValue(DragThumbTemplateProperty, value);
        }

        #endregion DragThumbTemplate Property

        #region ConnectorDecoratorTemplate Property

        // can be used to replace the default template for the ConnectorDecorator
        public static readonly DependencyProperty ConnectorDecoratorTemplateProperty =
            DependencyProperty.RegisterAttached("ConnectorDecoratorTemplate", typeof(ControlTemplate), typeof(Widget));

        public static ControlTemplate GetConnectorDecoratorTemplate(UIElement element)
        {
            return (ControlTemplate)element.GetValue(ConnectorDecoratorTemplateProperty);
        }

        public static void SetConnectorDecoratorTemplate(UIElement element, ControlTemplate value)
        {
            element.SetValue(ConnectorDecoratorTemplateProperty, value);
        }

        #endregion ConnectorDecoratorTemplate Property

        #region IsDragConnectionOver

        public static readonly DependencyProperty IsDragConnectionOverProperty =
                    DependencyProperty.Register("IsDragConnectionOver",
                                                 typeof(bool),
                                                 typeof(Widget),
                                                 new FrameworkPropertyMetadata(false));

        // while drag connection procedure is ongoing and the mouse moves over
        // this item this value is true; if true the ConnectorDecorator is triggered
        // to be visible, see template
        public bool IsDragConnectionOver
        {
            get { return (bool)GetValue(IsDragConnectionOverProperty); }
            set { SetValue(IsDragConnectionOverProperty, value); }
        }

        #endregion IsDragConnectionOver

        static Widget()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Widget), new FrameworkPropertyMetadata(typeof(Widget)));
        }

        public Widget()
        {
            Loaded += OnWidgetLoaded;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            WidgetPanel panel = VisualTreeHelper.GetParent(this) as WidgetPanel;

            // update selection
            if (panel != null)
                if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != ModifierKeys.None)
                    if (this.IsSelected)
                    {
                        this.IsSelected = false;
                        panel.SelectedItems.Remove(this);
                    }
                    else
                    {
                        this.IsSelected = true;
                        panel.SelectedItems.Add(this);
                    }
                else if (!this.IsSelected)
                {
                    foreach (ISelectable item in panel.SelectedItems)
                        item.IsSelected = false;

                    panel.SelectedItems.Clear();
                    this.IsSelected = true;
                    panel.SelectedItems.Add(this);
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
            if (base.Template != null)
            {
                ContentPresenter contentPresenter =
                    this.Template.FindName("PART_ContentPresenter", this) as ContentPresenter;
                if (contentPresenter != null)
                {
                    UIElement contentVisual = VisualTreeHelper.GetChild(contentPresenter, 0) as UIElement;
                    if (contentVisual != null)
                    {
                        DragThumb thumb = this.Template.FindName("PART_DragThumb", this) as DragThumb;
                        Control connectorDecorator = this.Template.FindName("PART_ConnectorDecorator", this) as Control;

                        if (thumb != null)
                        {
                            ControlTemplate template =
                                Widget.GetDragThumbTemplate(contentVisual) as ControlTemplate;
                            if (template != null)
                                thumb.Template = template;
                        }

                        if (connectorDecorator != null)
                        {
                            ControlTemplate template =
                                Widget.GetConnectorDecoratorTemplate(contentVisual) as ControlTemplate;
                            if (template != null)
                                connectorDecorator.Template = template;
                        }
                    }
                }
            }
        }
    }
}