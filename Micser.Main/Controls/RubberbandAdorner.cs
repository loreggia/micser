using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Micser.Main.Controls
{
    public class RubberbandAdorner : Adorner
    {
        private readonly Pen _pen;
        private readonly WidgetPanel _widgetPanel;
        private Point? _endPoint;
        private Point? _startPoint;

        public RubberbandAdorner(WidgetPanel adornedElement, Point startPoint)
            : base(adornedElement)
        {
            _widgetPanel = adornedElement;
            _startPoint = startPoint;
            _pen = new Pen(Brushes.LightSlateGray, 1);
            _pen.DashStyle = new DashStyle(new double[] { 2 }, 1);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!IsMouseCaptured)
                {
                    CaptureMouse();
                }

                _endPoint = e.GetPosition(this);
                UpdateSelection();
                InvalidateVisual();
            }
            else
            {
                if (IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }
            }

            e.Handled = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            // release mouse capture
            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }

            // remove this adorner from adorner layer
            var adornerLayer = AdornerLayer.GetAdornerLayer(_widgetPanel);
            adornerLayer?.Remove(this);

            e.Handled = true;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // without a background the OnMouseMove event would not be fired !
            // Alternative: implement a Canvas as a child of this adorner, like
            // the ConnectionAdorner does.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

            if (_startPoint.HasValue && _endPoint.HasValue)
            {
                dc.DrawRectangle(Brushes.Transparent, _pen, new Rect(_startPoint.Value, _endPoint.Value));
            }
        }

        private void UpdateSelection()
        {
            foreach (var item in _widgetPanel.SelectedItems)
            {
                item.IsSelected = false;
            }

            _widgetPanel.SelectedItems.Clear();

            if (_startPoint.HasValue && _endPoint.HasValue)
            {
                var rubberBand = new Rect(_startPoint.Value, _endPoint.Value);

                foreach (Control item in _widgetPanel.Children)
                {
                    var itemRect = VisualTreeHelper.GetDescendantBounds(item);
                    var itemBounds = item.TransformToAncestor(_widgetPanel).TransformBounds(itemRect);

                    if (rubberBand.Contains(itemBounds) && item is ISelectable)
                    {
                        var selectableItem = item as ISelectable;
                        selectableItem.IsSelected = true;
                        _widgetPanel.SelectedItems.Add(selectableItem);
                    }
                }
            }
        }
    }
}