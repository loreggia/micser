using Micser.Common.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Models;

namespace Micser.Engine.Infrastructure.DataAccess.Repositories
{
    /// <summary>
    /// Store for module connections.
    /// </summary>
    public interface IModuleConnectionRepository : IRepository<ModuleConnection>
    {
        /// <summary>
        /// Gets a connection between two modules.
        /// </summary>
        /// <param name="sourceId">The source module ID.</param>
        /// <param name="targetId">The target module ID.</param>
        /// <returns>The <see cref="ModuleConnection"/> between the modules or null if no connection exists.</returns>
        ModuleConnection GetBySourceAndTargetIds(long sourceId, long targetId);
    }
}