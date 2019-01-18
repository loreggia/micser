using System;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Micser.App.Infrastructure.Extensions;

namespace Micser.App.Infrastructure.Widgets
{
    public class DragThumb : Thumb
    {
        public DragThumb()
        {
            DragDelta += DragThumb_DragDelta;
        }

        private Widget ParentWidget => this.GetParentOfType<Widget>();

        private void DragThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var widget = ParentWidget;
            if (widget == null)
            {
                return;
            }

            if (VisualTreeHelper.GetParent(widget) is WidgetPanel panel && widget.IsSelected)
            {
                var minLeft = double.MaxValue;
                var minTop = double.MaxValue;

                // we only move Widgets
                var widgets = panel.Widgets.Where(w => w.IsSelected).ToArray();

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
        }
    }
}