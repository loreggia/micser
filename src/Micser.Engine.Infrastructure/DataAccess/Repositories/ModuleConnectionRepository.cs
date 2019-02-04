using Micser.Common.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Models;
using System.Data.Entity;

namespace Micser.Engine.Infrastructure.DataAccess.Repositories
{
    public class ModuleConnectionRepository : Repository<ModuleConnection>, IModuleConnectionRepository
    {
        public ModuleConnectionRepository(DbContext context)
            : base(context)
        {
        }
    }
}