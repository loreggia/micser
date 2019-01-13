using System.Collections.Generic;

namespace Micser.Common
{
    public interface IItemRegistry<T>
    {
        IEnumerable<T> Items { get; }

        void Add(T item);
    }
}