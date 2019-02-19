using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    ///     Represents a selectable item in the Toolbox.
    /// </summary>
    public class WidgetToolboxItem : ContentControl
    {
        private Point? _dragStartPoint;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _dragStartPoint = null;
            }

            if (_dragStartPoint.HasValue)
            {
                DragDrop.DoDragDrop(this, DataContext, DragDropEffects.Copy);
                //e.Handled = true;
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            _dragStartPoint = e.GetPosition(this);
        }
    }
}