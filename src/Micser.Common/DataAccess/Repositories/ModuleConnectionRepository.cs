using Micser.Common.DataAccess.Models;
using System.Data.Entity;

namespace Micser.Common.DataAccess.Repositories
{
    public class ModuleConnectionRepository : Repository<ModuleConnection>, IModuleConnectionRepository
    {
        public ModuleConnectionRepository(DbContext context)
            : base(context)
        {
        }
    }
}