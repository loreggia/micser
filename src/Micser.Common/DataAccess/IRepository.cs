using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Micser.Common.DataAccess
{
    public interface IRepository<TEntity>
        where TEntity : class
    {
        void Add(TEntity entity);

        void AddRange(IEnumerable<TEntity> entities);

        IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);

        TEntity Get(long id);

        IEnumerable<TEntity> GetAll();

        void Remove(TEntity entity);

        void RemoveRange(IEnumerable<TEntity> entities);
    }
}