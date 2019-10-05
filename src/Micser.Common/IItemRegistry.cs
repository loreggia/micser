using System.Collections.Generic;

namespace Micser.Common
{
    /// <summary>
    /// Generic item registry container.
    /// </summary>
    public interface IItemRegistry<T>
    {
        /// <summary>
        /// Gets the items stored in this registry.
        /// </summary>
        IEnumerable<T> Items { get; }

        /// <summary>
        /// Adds a new item to this registry. Does not check for duplicates.
        /// </summary>
        void Add(T item);
    }
}