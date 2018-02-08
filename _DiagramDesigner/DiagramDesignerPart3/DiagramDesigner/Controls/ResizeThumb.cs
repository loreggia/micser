using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DiagramDesigner.Controls
{
    public class ResizeThumb : Thumb
    {
        public ResizeThumb()
        {
            DragDelta += ResizeThumb_DragDelta;
        }

        private static void CalculateDragLimits(IEnumerable<ISelectable> selectedItems, out double minLeft, out double minTop, out double minDeltaHorizontal, out double minDeltaVertical)
        {
            minLeft = double.MaxValue;
            minTop = double.MaxValue;
            minDeltaHorizontal = double.MaxValue;
            minDeltaVertical = double.MaxValue;

            // drag limits are set by these parameters: canvas top, canvas left, minHeight, minWidth
            // calculate min value for each parameter for each item
            foreach (Widget item in selectedItems)
            {
                double left = Canvas.GetLeft(item);
                double top = Canvas.GetTop(item);

                minLeft = double.IsNaN(left) ? 0 : Math.Min(left, minLeft);
                minTop = double.IsNaN(top) ? 0 : Math.Min(top, minTop);

                minDeltaVertical = Math.Min(minDeltaVertical, item.ActualHeight - item.MinHeight);
                minDeltaHorizontal = Math.Min(minDeltaHorizontal, item.ActualWidth - item.MinWidth);
            }
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var widget = this.DataContext as Widget;
            var panel = VisualTreeHelper.GetParent(widget) as WidgetPanel;

            if (widget != null && panel != null && widget.IsSelected)
            {
                double minLeft, minTop, minDeltaHorizontal, minDeltaVertical;
                double dragDeltaVertical, dragDeltaHorizontal;

                // only resize Widgets
                var selectedItems = from item in panel.SelectedItems
                                    where item is Widget
                                    select item;

                CalculateDragLimits(selectedItems, out minLeft, out minTop,
                                    out minDeltaHorizontal, out minDeltaVertical);

                foreach (Widget item in selectedItems)
                {
                    if (item != null)
                    {
                        switch (base.VerticalAlignment)
                        {
                            case VerticalAlignment.Bottom:
                                dragDeltaVertical = Math.Min(-e.VerticalChange, minDeltaVertical);
                                item.Height = item.ActualHeight - dragDeltaVertical;
                                break;

                            case VerticalAlignment.Top:
                                double top = Canvas.GetTop(item);
                                dragDeltaVertical = Math.Min(Math.Max(-minTop, e.VerticalChange), minDeltaVertical);
                                Canvas.SetTop(item, top + dragDeltaVertical);
                                item.Height = item.ActualHeight - dragDeltaVertical;
                                break;

                            default:
                                break;
                        }

                        switch (base.HorizontalAlignment)
                        {
                            case HorizontalAlignment.Left:
                                double left = Canvas.GetLeft(item);
                                dragDeltaHorizontal = Math.Min(Math.Max(-minLeft, e.HorizontalChange), minDeltaHorizontal);
                                Canvas.SetLeft(item, left + dragDeltaHorizontal);
                                item.Width = item.ActualWidth - dragDeltaHorizontal;
                                break;

                            case HorizontalAlignment.Right:
                                dragDeltaHorizontal = Math.Min(-e.HorizontalChange, minDeltaHorizontal);
                                item.Width = item.ActualWidth - dragDeltaHorizontal;
                                break;

                            default:
                                break;
                        }
                    }
                }
                e.Handled = true;
            }
        }
    }
}