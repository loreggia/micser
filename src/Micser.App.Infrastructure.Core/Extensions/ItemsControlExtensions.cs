using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Extensions
{
    /// <summary>
    /// Provides helper extension methods for the <see cref="ItemsControl"/> class.
    /// </summary>
    public static class ItemsControlExtensions
    {
        /// <summary>
        /// Gets the child controls of an <see cref="ItemsControl"/>.
        /// </summary>
        public static IEnumerable<T> GetItemsChildren<T>(this ItemsControl itemsControl, string partName = null)
            where T : FrameworkElement
        {
            var result = new List<T>();

            for (var i = 0; i < itemsControl.Items.Count; i++)
            {
                var uiElement = itemsControl.ItemContainerGenerator.ContainerFromIndex(i);
                var child = uiElement.GetChildrenOfType<T>().FirstOrDefault(x => string.IsNullOrEmpty(partName) || x.Name == partName);
                result.Add(child);
            }

            return result.ToArray();
        }
    }
}