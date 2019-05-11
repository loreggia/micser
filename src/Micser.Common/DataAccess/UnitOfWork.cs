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
            _context.Dispose();
        }

        /// <inheritdoc />
        public T GetRepository<T>() where T : class, IRepository
        {
            return _repositoryFactory.Create<T>(_context);
        }
    }
}