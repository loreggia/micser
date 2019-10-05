using Microsoft.EntityFrameworkCore;
using Micser.Common.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Models;

namespace Micser.Engine.Infrastructure.DataAccess.Repositories
{
    /// <inheritdoc cref="IModuleRepository" />
    public class ModuleRepository : Repository<Module>, IModuleRepository
    {
        /// <inheritdoc />
        public ModuleRepository(DbContext context)
            : base(context)
        {
        }
    }
}