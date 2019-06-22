using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Micser.Common.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="ObservableCollection{T}"/> class.
    /// </summary>
    public static class ObservableCollectionExtensions
    {
        /// <summary>
        /// Updates the collection with current items.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="items">The current items.</param>
        /// <param name="equalityComparer">A function for determining item equality.</param>
        public static void Update<T>(this ObservableCollection<T> collection, IEnumerable<T> items, Func<T, T, bool> equalityComparer)
        {
            var itemArray = items.ToArray();

            for (var i = 0; i < collection.Count; i++)
            {
                var item = collection[i];
                if (!itemArray.Any(x => equalityComparer(x, item)))
                {
                    collection.RemoveAt(i);
                    i--;
                }
            }

            foreach (var item in itemArray)
            {
                if (!collection.Any(x => equalityComparer(x, item)))
                {
                    collection.Add(item);
                }
            }
        }
    }
}