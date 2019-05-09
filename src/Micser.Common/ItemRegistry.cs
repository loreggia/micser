﻿using System.Collections.Generic;
using System.Linq;

namespace Micser.Common
{
    /// <inheritdoc cref="IItemRegistry{T}"/> />
    public class ItemRegistry<T> : IItemRegistry<T>
    {
        private readonly IList<T> _items;

        public ItemRegistry()
        {
            _items = new List<T>();
        }

        public IEnumerable<T> Items => _items.AsEnumerable();

        public virtual void Add(T item)
        {
            if (item != null)
            {
                _items.Add(item);
            }
        }
    }
}