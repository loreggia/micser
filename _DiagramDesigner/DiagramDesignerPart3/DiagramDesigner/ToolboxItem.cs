using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace DiagramDesigner
{
    // Wraps info of the dragged object into a class
    public class DragObject
    {
        // Defines width and height of the Widget
        // when this DragObject is dropped on the WidgetPanel
        public Size? DesiredSize { get; set; }

        // Xaml string that represents the serialized content
        public String Xaml { get; set; }
    }

    // Represents a selectable item in the Toolbox/>.
    public class ToolboxItem : ContentControl
    {
        // caches the start point of the drag operation
        private Point? _dragStartPoint;

        static ToolboxItem()
        {
            // set the key to reference the style for this control
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolboxItem), new FrameworkPropertyMetadata(typeof(ToolboxItem)));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _dragStartPoint = null;
            }

            if (_dragStartPoint.HasValue)
            {
                // XamlWriter.Save() has limitations in exactly what is serialized,
                // see SDK documentation; short term solution only;
                var xamlString = XamlWriter.Save(Content);
                var dataObject = new DragObject
                {
                    Xaml = xamlString
                };

                if (VisualTreeHelper.GetParent(this) is WrapPanel panel)
                {
                    // desired size for WidgetPanel is the stretched Toolbox item size
                    var scale = 1.3;
                    dataObject.DesiredSize = new Size(panel.ItemWidth * scale, panel.ItemHeight * scale);
                }

                DragDrop.DoDragDrop(this, dataObject, DragDropEffects.Copy);

                e.Handled = true;
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            _dragStartPoint = e.GetPosition(this);
        }
    }
}