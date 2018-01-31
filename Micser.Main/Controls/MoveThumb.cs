using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Micser.Main.Controls
{
    public class MoveThumb : Thumb
    {
        public MoveThumb()
        {
            DragDelta += MoveThumb_DragDelta;
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is UIElement designerItem)
            {
                var left = Canvas.GetLeft(designerItem);
                var top = Canvas.GetTop(designerItem);

                Canvas.SetLeft(designerItem, left + e.HorizontalChange);
                Canvas.SetTop(designerItem, top + e.VerticalChange);
            }
        }
    }
}