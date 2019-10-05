using Microsoft.EntityFrameworkCore;
using System;

namespace Micser.Common.DataAccess
{
    /// <inheritdoc cref="IUnitOfWork" />
    public class UnitOfWork : IUnitOfWork
    {
        protected readonly DbContext Context;
        protected readonly IRepositoryFactory RepositoryFactory;

        /// <inheritdoc />
        public UnitOfWork(DbContext context, IRepositoryFactory repositoryFactory)
        {
            Context = context;
            RepositoryFactory = repositoryFactory;
        }

        /// <inheritdoc />
        public int Complete()
        {
            return Context.SaveChanges();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public T GetRepository<T>() where T : class, IRepository
        {
            return RepositoryFactory.Create<T>(Context);
        }

        /// <summary>
        /// Disposes the context.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            Context?.Dispose();
        }
    }
}