using Micser.Common.DataAccess.Repositories;
using System.Data.Entity;

namespace Micser.Common.DataAccess
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private IModuleConnectionRepository _moduleConnections;
        private IModuleRepository _modules;

        public UnitOfWork(DbContext context)
        {
            _context = context;
        }

        public IModuleConnectionRepository ModuleConnections => _moduleConnections ?? (_moduleConnections = new ModuleConnectionRepository(_context));
        public IModuleRepository Modules => _modules ?? (_modules = new ModuleRepository(_context));

        public int Complete()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}