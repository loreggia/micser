using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Micser.Main.Controls
{
    public class Connector : Control
    {
        private Point? _dragStartPoint;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _dragStartPoint = null;
            }

            // but if mouse button is pressed and start point value is set we do have one
            if (_dragStartPoint.HasValue)
            {
                // create connection adorner
                var panel = GetWidgetPanel(this);
                if (panel != null)
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(panel);
                    if (adornerLayer != null)
                    {
                        var adorner = new ConnectorAdorner(panel, this);
                        adornerLayer.Add(adorner);
                        e.Handled = true;
                    }
                }
            }
        }

        private WidgetPanel GetWidgetPanel(DependencyObject element)
        {
            while (element != null && !(element is WidgetPanel))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            return element as WidgetPanel;
        }
    }
}