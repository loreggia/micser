using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Micser.Infrastructure.Extensions;

namespace Micser.Infrastructure.Widgets
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

            foreach (var widget in widgets)
            {
                var position = widget.Position;

                minLeft = Math.Min(position.X, minLeft);
                minTop = Math.Min(position.Y, minTop);

                minDeltaVertical = Math.Min(minDeltaVertical, widget.ActualHeight - widget.MinHeight);
                minDeltaHorizontal = Math.Min(minDeltaHorizontal, widget.ActualWidth - widget.MinWidth);
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
                var widgets = panel.Widgets.Where(w => w.IsSelected).ToArray();

                CalculateDragLimits(widgets, out var minLeft, out var minTop, out var minDeltaHorizontal, out var minDeltaVertical);

                foreach (var w in widgets)
                {
                    if (w == null)
                    {
                        continue;
                    }

                    double dragDeltaVertical;
                    switch (VerticalAlignment)
                    {
                        case VerticalAlignment.Bottom:
                            dragDeltaVertical = Math.Min(-e.VerticalChange, minDeltaVertical);
                            w.Height = w.ActualHeight - dragDeltaVertical;
                            break;

                        case VerticalAlignment.Top:
                            var position = w.Position;
                            dragDeltaVertical = Math.Min(Math.Max(-minTop, e.VerticalChange), minDeltaVertical);
                            position.Y += dragDeltaVertical;
                            w.Position = position;
                            w.Height = w.ActualHeight - dragDeltaVertical;
                            break;
                    }

                    double dragDeltaHorizontal;
                    switch (HorizontalAlignment)
                    {
                        case HorizontalAlignment.Left:
                            var position = w.Position;
                            dragDeltaHorizontal = Math.Min(Math.Max(-minLeft, e.HorizontalChange), minDeltaHorizontal);
                            position.X += dragDeltaHorizontal;
                            w.Position = position;
                            w.Width = w.ActualWidth - dragDeltaHorizontal;
                            break;

                        case HorizontalAlignment.Right:
                            dragDeltaHorizontal = Math.Min(-e.HorizontalChange, minDeltaHorizontal);
                            w.Width = w.ActualWidth - dragDeltaHorizontal;
                            break;
                    }
                }
                e.Handled = true;
            }
        }
    }
}