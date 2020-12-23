using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Micser.Common.Modules;
using Micser.Common.Services;
using Micser.DataAccess;
using Micser.DataAccess.Entities;
using Micser.UI.Infrastructure;

namespace Micser.Services
{
    /// <inheritdoc cref="IModuleConnectionService"/>
    public class ModuleConnectionService : IModuleConnectionService
    {
        private readonly IDbContextFactory<EngineDbContext> _dbContextFactory;

        /// <inheritdoc />
        public ModuleConnectionService(IDbContextFactory<EngineDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <inheritdoc />
        public async Task<ModuleConnectionDto?> DeleteAsync(long id)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var connection = await dbContext.ModuleConnections.FindAsync(id).ConfigureAwait(false);

            if (connection == null)
            {
                return null;
            }

            dbContext.ModuleConnections.Remove(connection);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return GetModuleConnectionDto(connection);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ModuleConnectionDto> GetAllAsync()
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();

            await foreach (var connection in dbContext.ModuleConnections)
            {
                yield return GetModuleConnectionDto(connection)!;
            }
        }

        /// <inheritdoc />
        public async Task<ModuleConnectionDto?> GetByIdAsync(long id)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var connection = await dbContext.ModuleConnections.FindAsync(id).ConfigureAwait(false);
            return connection == null ? null : GetModuleConnectionDto(connection);
        }

        /// <inheritdoc />
        public async Task InsertAsync(ModuleConnectionDto dto)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var connection = await dbContext
                .ModuleConnections
                .SingleOrDefaultAsync(c => c.SourceModuleId == dto.SourceId && c.TargetModuleId == dto.TargetId)
                .ConfigureAwait(false);

            if (connection != null)
            {
                return;
            }

            connection = GetModuleConnection(dto);
            await dbContext.ModuleConnections.AddAsync(connection!).ConfigureAwait(false);

            if (await dbContext.SaveChangesAsync().ConfigureAwait(false) > 0)
            {
                dto.Id = connection.Id;
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
        public async Task UpdateAsync(ModuleConnectionDto dto)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var connection = await dbContext.ModuleConnections.FindAsync(dto.Id).ConfigureAwait(false);

            if (connection == null)
            {
                throw new NotFoundApiException();
            }

            connection.SourceModuleId = dto.SourceId;
            connection.TargetModuleId = dto.TargetId;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private static ModuleConnection GetModuleConnection(ModuleConnectionDto dto)
        {
            return new ModuleConnection
            {
                Id = dto.Id,
                SourceConnectorName = dto.SourceConnectorName,
                SourceModuleId = dto.SourceId,
                TargetConnectorName = dto.TargetConnectorName,
                TargetModuleId = dto.TargetId
            };
        }

        private static ModuleConnectionDto GetModuleConnectionDto(ModuleConnection mc)
        {
            return new ModuleConnectionDto
            {
                Id = mc.Id,
                SourceConnectorName = mc.SourceConnectorName,
                SourceId = mc.SourceModuleId,
                TargetConnectorName = mc.TargetConnectorName,
                TargetId = mc.TargetModuleId
            };
        }
    }
}