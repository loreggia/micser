using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Micser.Infrastructure.Extensions;

namespace Micser.Infrastructure.Controls
{
    public class ResizeThumb : Thumb
    {
        public ResizeThumb()
        {
            DragDelta += ResizeThumb_DragDelta;
        }

        private Widget ParentWidget => this.GetParentOfType<Widget>();

        private static void CalculateDragLimits(IEnumerable<ISelectable> selectedItems, out double minLeft, out double minTop, out double minDeltaHorizontal, out double minDeltaVertical)
        {
            minLeft = double.MaxValue;
            minTop = double.MaxValue;
            minDeltaHorizontal = double.MaxValue;
            minDeltaVertical = double.MaxValue;

            // drag limits are set by these parameters: canvas top, canvas left, minHeight, minWidth
            // calculate min value for each parameter for each item
            var widgets = selectedItems.OfType<Widget>();

            foreach (var item in widgets)
            {
                var left = Canvas.GetLeft(item);
                var top = Canvas.GetTop(item);

                minLeft = double.IsNaN(left) ? 0 : Math.Min(left, minLeft);
                minTop = double.IsNaN(top) ? 0 : Math.Min(top, minTop);

                minDeltaVertical = Math.Min(minDeltaVertical, item.ActualHeight - item.MinHeight);
                minDeltaHorizontal = Math.Min(minDeltaHorizontal, item.ActualWidth - item.MinWidth);
            }
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var widget = ParentWidget;
            if (widget == null)
            {
                return;
            }

            if (VisualTreeHelper.GetParent(widget) is WidgetPanel panel && widget.IsSelected)
            {
                // only resize Widgets
                var selectedItems = panel.SelectedItems.OfType<Widget>().ToArray();

                CalculateDragLimits(selectedItems, out var minLeft, out var minTop, out var minDeltaHorizontal, out var minDeltaVertical);

                foreach (var item in selectedItems)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    double dragDeltaVertical;
                    switch (VerticalAlignment)
                    {
                        case VerticalAlignment.Bottom:
                            dragDeltaVertical = Math.Min(-e.VerticalChange, minDeltaVertical);
                            item.Height = item.ActualHeight - dragDeltaVertical;
                            break;

                        case VerticalAlignment.Top:
                            var top = Canvas.GetTop(item);
                            dragDeltaVertical = Math.Min(Math.Max(-minTop, e.VerticalChange), minDeltaVertical);
                            Canvas.SetTop(item, top + dragDeltaVertical);
                            item.Height = item.ActualHeight - dragDeltaVertical;
                            break;
                    }

                    double dragDeltaHorizontal;
                    switch (HorizontalAlignment)
                    {
                        case HorizontalAlignment.Left:
                            var left = Canvas.GetLeft(item);
                            dragDeltaHorizontal = Math.Min(Math.Max(-minLeft, e.HorizontalChange), minDeltaHorizontal);
                            Canvas.SetLeft(item, left + dragDeltaHorizontal);
                            item.Width = item.ActualWidth - dragDeltaHorizontal;
                            break;

                        case HorizontalAlignment.Right:
                            dragDeltaHorizontal = Math.Min(-e.HorizontalChange, minDeltaHorizontal);
                            item.Width = item.ActualWidth - dragDeltaHorizontal;
                            break;
                    }
                }
                e.Handled = true;
            }
        }
    }
}