using System.Collections;
using System.Collections.Generic;

namespace Micser.Common.DataAccess
{
    public interface IDbSet<T> : IDbSet, IEnumerable<T>
    {
        void Delete<TId>(TId id);

        T GetById<TId>(TId id);

        void Insert(T entity);

        void Update(T entity);
    }

    public interface IDbSet : IEnumerable
    {
        string Name { get; }
    }
}