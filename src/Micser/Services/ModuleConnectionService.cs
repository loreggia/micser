using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Micser.Common.Modules;
using Micser.Common.Services;
using Micser.DataAccess;
using Micser.DataAccess.Entities;
using Micser.Infrastructure;

namespace Micser.Services
{
    /// <inheritdoc cref="IModuleConnectionService"/>
    public class ModuleConnectionService : IModuleConnectionService
    {
        private readonly IDbContextFactory<EngineDbContext> _dbContextFactory;

        public ModuleConnectionService(IDbContextFactory<EngineDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <inheritdoc />
        public async Task<ModuleConnection?> DeleteAsync(long id)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var entity = await dbContext.ModuleConnections.FindAsync(id).ConfigureAwait(false);

            if (entity == null)
            {
                return null;
            }

            dbContext.ModuleConnections.Remove(entity);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return ToModel(entity);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ModuleConnection> GetAllAsync()
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();

            await foreach (var entity in dbContext.ModuleConnections)
            {
                yield return ToModel(entity)!;
            }
        }

        /// <inheritdoc />
        public async Task<ModuleConnection?> GetByIdAsync(long id)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var entity = await dbContext.ModuleConnections.FindAsync(id).ConfigureAwait(false);
            return entity == null ? null : ToModel(entity);
        }

        /// <inheritdoc />
        public async Task InsertAsync(ModuleConnection mc)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var existingEntity = await dbContext
                .ModuleConnections
                .SingleOrDefaultAsync(c => c.SourceModuleId == mc.SourceId && c.TargetModuleId == mc.TargetId)
                .ConfigureAwait(false);

            if (existingEntity != null)
            {
                return;
            }

            existingEntity = ToEntity(mc);
            await dbContext.ModuleConnections.AddAsync(existingEntity!).ConfigureAwait(false);

            if (await dbContext.SaveChangesAsync().ConfigureAwait(false) > 0)
            {
                mc.Id = existingEntity.Id;
            }
        }

        /// <inheritdoc />
        public async Task TruncateAsync()
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();

            await foreach (var connection in dbContext.ModuleConnections)
            {
                dbContext.ModuleConnections.Remove(connection);
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(ModuleConnection mc)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var entity = await dbContext.ModuleConnections.FindAsync(mc.Id).ConfigureAwait(false);

            if (entity == null)
            {
                throw new NotFoundApiException();
            }

            entity.SourceModuleId = mc.SourceId;
            entity.TargetModuleId = mc.TargetId;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private static ModuleConnectionEntity ToEntity(ModuleConnection dto)
        {
            return new ModuleConnectionEntity
            {
                Id = dto.Id,
                SourceConnectorName = dto.SourceConnectorName,
                SourceModuleId = dto.SourceId,
                TargetConnectorName = dto.TargetConnectorName,
                TargetModuleId = dto.TargetId
            };
        }

        private static ModuleConnection ToModel(ModuleConnectionEntity mc)
        {
            return new ModuleConnection(mc.Id, mc.SourceModuleId, mc.SourceConnectorName, mc.TargetModuleId, mc.TargetConnectorName);
        }
    }
}