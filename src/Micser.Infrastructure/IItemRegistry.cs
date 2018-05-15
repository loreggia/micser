using System.Collections.Generic;

namespace Micser.Infrastructure
{
    public interface IItemRegistry<T>
    {
        IEnumerable<T> Items { get; }

        void Add(T item);
    }
}