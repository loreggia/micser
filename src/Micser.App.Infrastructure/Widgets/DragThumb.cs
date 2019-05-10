using Micser.App.Infrastructure.Extensions;
using System;
using System.Linq;
using System.Windows.Controls.Primitives;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// A <see cref="Thumb"/> control handling moving of a widget.
    /// </summary>
    public class DragThumb : Thumb
    {
        public DragThumb()
        {
            DragDelta += DragThumb_DragDelta;
            DragStarted += DragThumb_DragStarted;
            DragCompleted += DragThumb_DragCompleted;
        }

        private Widget Widget => this.GetParentOfType<Widget>();

        private WidgetPanel WidgetPanel => this.GetParentOfType<WidgetPanel>();

        private void DragThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            WidgetPanel.IsGridVisible = false;
        }

        private void DragThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var widget = Widget;
            var panel = WidgetPanel;

            if (widget == null || WidgetPanel == null || !widget.IsSelected)
            {
                return;
            }

            var minLeft = double.MaxValue;
            var minTop = double.MaxValue;

            // we only move Widgets
            var widgets = WidgetPanel.Widgets.Where(w => w.IsSelected).ToArray();

            foreach (var w in widgets)
            {
                var position = w.Position;

                minLeft = Math.Min(position.X, minLeft);
                minTop = Math.Min(position.Y, minTop);
            }

            var deltaHorizontal = Math.Max(-minLeft, e.HorizontalChange);
            var deltaVertical = Math.Max(-minTop, e.VerticalChange);

            foreach (var w in widgets)
            {
                var position = w.Position;
                position.X += deltaHorizontal;
                position.Y += deltaVertical;
                w.Position = position;
            }

            panel.InvalidateMeasure();
            e.Handled = true;
        }

        private void DragThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            Widget.Focus();
            WidgetPanel.IsGridVisible = true;
        }
    }
}