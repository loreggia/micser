using System.Collections.Generic;

namespace Micser.Common.DataAccess
{
    public interface IDbSet<T> : IEnumerable<T>
    {
        void Insert(T entity);
        void Update(T entity);
    }
}