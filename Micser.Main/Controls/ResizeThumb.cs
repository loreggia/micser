using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Micser.Infrastructure.Extensions;

namespace Micser.Main.Controls
{
    public class ResizeThumb : Thumb
    {
        public ResizeThumb()
        {
            DragDelta += ResizeThumb_DragDelta;
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is FrameworkElement element)
            {
                element.EnsureCanvasTopLeft();

                double deltaVertical, deltaHorizontal;

                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                        deltaVertical = Math.Min(-e.VerticalChange, element.ActualHeight - element.MinHeight);
                        element.Height -= deltaVertical;
                        break;

                    case VerticalAlignment.Top:
                        deltaVertical = Math.Min(e.VerticalChange, element.ActualHeight - element.MinHeight);
                        Canvas.SetTop(element, Canvas.GetTop(element) + deltaVertical);
                        element.Height -= deltaVertical;
                        break;
                }

                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        deltaHorizontal = Math.Min(e.HorizontalChange, element.ActualWidth - element.MinWidth);
                        Canvas.SetLeft(element, Canvas.GetLeft(element) + deltaHorizontal);
                        element.Width -= deltaHorizontal;
                        break;

                    case HorizontalAlignment.Right:
                        deltaHorizontal = Math.Min(-e.HorizontalChange, element.ActualWidth - element.MinWidth);
                        element.Width -= deltaHorizontal;
                        break;
                }
            }

            e.Handled = true;
        }
    }
}