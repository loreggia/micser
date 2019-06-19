using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Micser.Common.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static void Update<T>(this ObservableCollection<T> collection, IEnumerable<T> items, Func<T, T, bool> equalityComparer)
        {
            var itemArray = items.ToArray();

            for (var i = 0; i < collection.Count; i++)
            {
                var item = collection[i];
                if (!items.Any(x => equalityComparer(x, item)))
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