using Micser.Common.DataAccess.Models;
using System.Data.Entity;

namespace Micser.Common.DataAccess.Repositories
{
    public class ModuleRepository : Repository<Module>, IModuleRepository
    {
        public ModuleRepository(DbContext context)
            : base(context)
        {
        }
    }
}