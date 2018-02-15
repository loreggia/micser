using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace CanvasTest
{
    public class DragThumb : Thumb
    {
        public DragThumb()
        {
            DragDelta += DragThumb_DragDelta;
            Cursor = Cursors.SizeAll;
        }

        private void DragThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var widget = this.GetParentOfType<Widget>();

            var position = widget.Position;
            position.X += e.HorizontalChange;
            position.Y += e.VerticalChange;
            widget.Position = position;
        }
    }
}