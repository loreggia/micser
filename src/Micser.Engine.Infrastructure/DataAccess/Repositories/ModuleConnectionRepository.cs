using Micser.Common.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Models;
using System.Data.Entity;
using System.Linq;

namespace Micser.Engine.Infrastructure.DataAccess.Repositories
{
    public class ModuleConnectionRepository : Repository<ModuleConnection>, IModuleConnectionRepository
    {
        public ModuleConnectionRepository(DbContext context)
            : base(context)
        {
        }

        public ModuleConnection GetBySourceAndTargetIds(long sourceId, long targetId)
        {
            return DbSet.SingleOrDefault(c => c.SourceModuleId == sourceId && c.TargetModuleId == targetId);
        }
    }
}