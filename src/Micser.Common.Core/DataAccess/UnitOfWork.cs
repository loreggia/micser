using System;
using System.Data.Entity;

namespace Micser.Common.DataAccess
{
    /// <inheritdoc cref="IUnitOfWork" />
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private readonly IRepositoryFactory _repositoryFactory;

        /// <inheritdoc />
        public UnitOfWork(DbContext context, IRepositoryFactory repositoryFactory)
        {
            _context = context;
            _repositoryFactory = repositoryFactory;
        }

        /// <inheritdoc />
        public int Complete()
        {
            return _context.SaveChanges();
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
            return _repositoryFactory.Create<T>(_context);
        }

        /// <summary>
        /// Disposes the context.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            _context?.Dispose();
        }
    }
}