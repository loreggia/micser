﻿using Microsoft.EntityFrameworkCore;
using Micser.Common.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Models;
using System.Linq;

namespace Micser.Engine.Infrastructure.DataAccess.Repositories
{
    /// <inheritdoc cref="IModuleConnectionRepository" />
    public class ModuleConnectionRepository : Repository<ModuleConnection>, IModuleConnectionRepository
    {
        /// <inheritdoc />
        public ModuleConnectionRepository(DbContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public ModuleConnection GetBySourceAndTargetIds(long sourceId, long targetId)
        {
            return DbSet.SingleOrDefault(c => c.SourceModuleId == sourceId && c.TargetModuleId == targetId);
        }
    }
}