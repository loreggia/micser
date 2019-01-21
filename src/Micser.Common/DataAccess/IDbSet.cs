using System.Collections;
using System.Collections.Generic;

namespace Micser.Common.DataAccess
{
    public interface IDbSet<T> : IDbSet, IEnumerable<T>
    {
        void Insert(T entity);
        void Update(T entity);
        T GetById<TId>(TId id);
    }

    public interface IDbSet : IEnumerable
    {
        string Name { get; }
    }
}
