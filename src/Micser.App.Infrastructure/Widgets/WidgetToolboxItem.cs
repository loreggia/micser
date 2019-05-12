using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Represents a selectable item in the Toolbox.
    /// </summary>
    public class WidgetToolboxItem : ContentControl
    {
        private Point? _dragStartPoint;

        /// <summary>
        /// Handles drag &amp; drop.
        /// </summary>
        /// <param name="e"></param>
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

        /// <summary>
        /// Starts drag &amp; drop.
        /// </summary>
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            _dragStartPoint = e.GetPosition(this);
        }
    }
}