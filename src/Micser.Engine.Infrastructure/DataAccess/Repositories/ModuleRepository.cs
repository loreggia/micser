using Micser.Common.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Models;
using System.Data.Entity;

namespace Micser.Engine.Infrastructure.DataAccess.Repositories
{
    public class ModuleRepository : Repository<Module>, IModuleRepository
    {
        public ModuleRepository(DbContext context)
            : base(context)
        {
        }
    }
}