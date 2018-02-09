using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Micser.Infrastructure.Extensions;

namespace Micser.Infrastructure.Controls
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
                var widgets = panel.SelectedItems.OfType<Widget>().ToArray();

                foreach (var item in widgets)
                {
                    var left = Canvas.GetLeft(item);
                    var top = Canvas.GetTop(item);

                    minLeft = double.IsNaN(left) ? 0 : Math.Min(left, minLeft);
                    minTop = double.IsNaN(top) ? 0 : Math.Min(top, minTop);
                }

                var deltaHorizontal = Math.Max(-minLeft, e.HorizontalChange);
                var deltaVertical = Math.Max(-minTop, e.VerticalChange);

                foreach (var item in widgets)
                {
                    var left = Canvas.GetLeft(item);
                    var top = Canvas.GetTop(item);

                    if (double.IsNaN(left))
                    {
                        left = 0;
                    }

                    if (double.IsNaN(top))
                    {
                        top = 0;
                    }

                    Canvas.SetLeft(item, left + deltaHorizontal);
                    Canvas.SetTop(item, top + deltaVertical);
                }

                panel.InvalidateMeasure();
                e.Handled = true;
            }
        }
    }
}