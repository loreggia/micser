using System.Windows;
using System.Windows.Media;

namespace CanvasTest
{
    public static class DependencyObjectExtensions
    {
        public static T GetParentOfType<T>(this DependencyObject element)
            where T : UIElement
        {
            while (element != null && !(element is T))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            return element as T;
        }
    }
}