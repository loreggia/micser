using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace Micser.Common.DataAccess
{
    /// <summary>
    /// Base implementation of a generic repository.
    /// </summary>
    /// <typeparam name="TEntity">The model type.</typeparam>
    public abstract class Repository<TEntity> : IRepository<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// The EF database context.
        /// </summary>
        protected readonly DbContext Context;

        /// <summary>
        /// Creates an instance of the <see cref="Repository{TEntity}"/> class.
        /// </summary>
        /// <param name="context"></param>
        protected Repository(DbContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Gets the <see cref="DbSet{TEntity}"/> for the current model type.
        /// </summary>
        protected DbSet<TEntity> DbSet => Context.Set<TEntity>();

        /// <inheritdoc />
        public void Add(TEntity entity)
        {
            DbSet.Add(entity);
        }

        /// <inheritdoc />
        public void AddRange(IEnumerable<TEntity> entities)
        {
            DbSet.AddRange(entities);
        }

        /// <inheritdoc />
        public IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        {
            return DbSet.Where(predicate);
        }

        /// <inheritdoc />
        public TEntity Get(long id)
        {
            return DbSet.Find(id);
        }

        /// <inheritdoc />
        public IEnumerable<TEntity> GetAll()
        {
            return DbSet.ToList();
        }

        /// <inheritdoc />
        public void Remove(TEntity entity)
        {
            DbSet.Remove(entity);
        }

        /// <inheritdoc />
        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            DbSet.RemoveRange(entities);
        }
    }
}