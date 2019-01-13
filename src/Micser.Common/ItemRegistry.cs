using System.Collections.Generic;
using System.Linq;

namespace Micser.Common
{
    public class ItemRegistry<T> : IItemRegistry<T>
    {
        private readonly IList<T> _items;

        public ItemRegistry()
        {
            _items = new List<T>();
        }

        public IEnumerable<T> Items => _items.AsEnumerable();

        public void Add(T item)
        {
            if (item != null)
            {
                _items.Add(item);
            }
        }
    }
}