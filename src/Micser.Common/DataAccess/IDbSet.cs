using System.Collections;
using System.Collections.Generic;

namespace Micser.Common.DataAccess
{
    public interface IDbSet<T> : IDbSet, IEnumerable<T>
    {
        void Insert(T entity);
        void Update(T entity);
    }

    public interface IDbSet : IEnumerable
    {
        string Name { get; }
    }
}