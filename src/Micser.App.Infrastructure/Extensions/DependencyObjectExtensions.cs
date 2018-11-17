using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Micser.App.Infrastructure.Extensions
{
    public static class DependencyObjectExtensions
    {
        public static IEnumerable<T> GetChildrenOfType<T>(this DependencyObject element)
            where T : UIElement
        {
            var result = new List<T>();

            if (element == null)
            {
                return new T[0];
            }

            if (element is T tElement)
            {
                result.Add(tElement);
            }

            var count = VisualTreeHelper.GetChildrenCount(element);

            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                var children = GetChildrenOfType<T>(child);
                result.AddRange(children);
            }

            return result.ToArray();
        }

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