using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Micser.Common.DataAccess
{
    /// <summary>
    /// Database repository base interface.
    /// </summary>
    public interface IRepository
    {
    }

    /// <summary>
    /// A generic repository.
    /// </summary>
    public interface IRepository<TEntity> : IRepository
        where TEntity : class
    {
        /// <summary>
        /// Adds an entity to the repository.
        /// </summary>
        void Add(TEntity entity);

        /// <summary>
        /// Adds multiple entities to the repository.
        /// </summary>
        void AddRange(IEnumerable<TEntity> entities);

        /// <summary>
        /// Finds entities in this repository using a predicate expression.
        /// </summary>
        IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Gets an entity with the specified ID.
        /// </summary>
        TEntity Get(long id);

        /// <summary>
        /// Gets all entities stored in the repository.
        /// </summary>
        IEnumerable<TEntity> GetAll();

        /// <summary>
        /// Removes an entity from the repository.
        /// </summary>
        void Remove(TEntity entity);

        /// <summary>
        /// Removes multiple entities from the repository.
        /// </summary>
        void RemoveRange(IEnumerable<TEntity> entities);
    }
}