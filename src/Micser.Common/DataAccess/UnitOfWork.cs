using System.Data.Entity;

namespace Micser.Common.DataAccess
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private readonly IRepositoryFactory _repositoryFactory;

        public UnitOfWork(DbContext context, IRepositoryFactory repositoryFactory)
        {
            _context = context;
            _repositoryFactory = repositoryFactory;
        }

        public int Complete()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public T GetRepository<T>() where T : class, IRepository
        {
            return _repositoryFactory.Create<T>(_context);
        }
    }
}