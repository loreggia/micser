using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Micser.Main.Controls
{
    public class MoveThumb : Thumb
    {
        public static readonly DependencyProperty AllowNegativeProperty = DependencyProperty.Register(
            nameof(AllowNegative), typeof(bool), typeof(MoveThumb), new PropertyMetadata(false));

        public MoveThumb()
        {
            DragDelta += MoveThumb_DragDelta;
        }

        public bool AllowNegative
        {
            get => (bool)GetValue(AllowNegativeProperty);
            set => SetValue(AllowNegativeProperty, value);
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is UIElement designerItem)
            {
                var left = Canvas.GetLeft(designerItem) + e.HorizontalChange;
                var top = Canvas.GetTop(designerItem) + e.VerticalChange;

                if (!AllowNegative)
                {
                    if (left < 0)
                    {
                        left = 0;
                    }

                    if (top < 0)
                    {
                        top = 0;
                    }
                }

                Canvas.SetLeft(designerItem, left);
                Canvas.SetTop(designerItem, top);
            }
        }
    }
}