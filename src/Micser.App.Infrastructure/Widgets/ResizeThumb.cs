using Micser.App.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// A <see cref="Thumb"/> control handling widget resizing.
    /// </summary>
    public class ResizeThumb : Thumb
    {
        /// <inheritdoc />
        public ResizeThumb()
        {
            DragDelta += ResizeThumb_DragDelta;
            DragStarted += ResizeThumb_DragStarted;
            DragCompleted += ResizeThumb_DragCompleted;
        }

        private Widget Widget => this.GetParentOfType<Widget>();

        private WidgetPanel WidgetPanel => this.GetParentOfType<WidgetPanel>();

        private static void CalculateDragLimits(IEnumerable<ISelectable> selectedItems, out double minLeft, out double minTop,
                                                out double minDeltaHorizontal, out double minDeltaVertical)
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

        private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            WidgetPanel.IsGridVisible = false;
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var widget = Widget;
            var panel = WidgetPanel;

            if (widget == null || WidgetPanel == null || !widget.IsSelected)
            {
                return;
            }

            var widgets = panel.Widgets.Where(w => w.IsSelected).ToArray();

            CalculateDragLimits(widgets, out var minLeft, out var minTop, out var minDeltaHorizontal, out var minDeltaVertical);

            foreach (var w in widgets)
            {
                if (w == null)
                {
                    continue;
                }

                double dragDeltaVertical;
                var size = w.Size;
                var position = w.Position;

                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                        dragDeltaVertical = Math.Min(-e.VerticalChange, minDeltaVertical);
                        size.Height = w.ActualHeight - dragDeltaVertical;
                        break;

                    case VerticalAlignment.Top:
                        dragDeltaVertical = Math.Min(Math.Max(-minTop, e.VerticalChange), minDeltaVertical);
                        position.Y += dragDeltaVertical;
                        size.Height = w.ActualHeight - dragDeltaVertical;
                        break;
                }

                double dragDeltaHorizontal;
                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        dragDeltaHorizontal = Math.Min(Math.Max(-minLeft, e.HorizontalChange), minDeltaHorizontal);
                        position.X += dragDeltaHorizontal;
                        size.Width = w.ActualWidth - dragDeltaHorizontal;
                        break;

                    case HorizontalAlignment.Right:
                        dragDeltaHorizontal = Math.Min(-e.HorizontalChange, minDeltaHorizontal);
                        size.Width = w.ActualWidth - dragDeltaHorizontal;
                        break;
                }

                w.Position = position;
                w.Size = size;
            }

            //e.Handled = true;
        }

        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            WidgetPanel.IsGridVisible = true;
        }
    }
}