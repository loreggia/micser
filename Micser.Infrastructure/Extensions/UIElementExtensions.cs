using System.Windows;
using System.Windows.Controls;

namespace Micser.Infrastructure.Extensions
{
    public static class UIElementExtensions
    {
        public static void EnsureCanvasTopLeft(this UIElement element)
        {
            if (double.IsNaN(Canvas.GetTop(element)))
            {
                Canvas.SetTop(element, 0d);
            }
            if (double.IsNaN(Canvas.GetLeft(element)))
            {
                Canvas.SetLeft(element, 0d);
            }
        }
    }
}