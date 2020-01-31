using Micser.Common.DataAccess;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Models;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Infrastructure.Services
{
    /// <inheritdoc cref="IModuleConnectionService"/>
    public class ModuleConnectionService : IModuleConnectionService
    {
        private readonly IDbContextFactory _dbContextFactory;

        /// <inheritdoc />
        public ModuleConnectionService(IDbContextFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <inheritdoc />
        public ModuleConnectionDto Delete(long id)
        {
            using var context = _dbContextFactory.Create<EngineDbContext>();
            var connection = context.ModuleConnections.Find(id);

            if (connection == null)
            {
                return null;
            }

            context.ModuleConnections.Remove(connection);
            context.SaveChanges();

            return GetModuleConnectionDto(connection);
        }

        /// <inheritdoc />
        public IEnumerable<ModuleConnectionDto> GetAll()
        {
            using var dbContext = _dbContextFactory.Create<EngineDbContext>();
            return dbContext.ModuleConnections.AsEnumerable().Select(GetModuleConnectionDto).ToArray();
        }

        /// <inheritdoc />
        public ModuleConnectionDto GetById(long id)
        {
            using var dbContext = _dbContextFactory.Create<EngineDbContext>();
            var connection = dbContext.ModuleConnections.Find(id);
            return GetModuleConnectionDto(connection);
        }

        /// <inheritdoc />
        public bool Insert(ModuleConnectionDto dto)
        {
            using var dbContext = _dbContextFactory.Create<EngineDbContext>();
            var connection = dbContext.ModuleConnections.SingleOrDefault(c => c.SourceModuleId == dto.SourceId && c.TargetModuleId == dto.TargetId);

            if (connection != null)
            {
                return false;
            }

            connection = GetModuleConnection(dto);
            dbContext.ModuleConnections.Add(connection);

            if (dbContext.SaveChanges() > 0)
            {
                dto.Id = connection.Id;
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool Truncate()
        {
            using var dbContext = _dbContextFactory.Create<EngineDbContext>();
            var connections = dbContext.ModuleConnections.AsEnumerable();
            dbContext.ModuleConnections.RemoveRange(connections);
            return dbContext.SaveChanges() >= 0;
        }

        /// <inheritdoc />
        public bool Update(ModuleConnectionDto dto)
        {
            using var dbContext = _dbContextFactory.Create<EngineDbContext>();
            var connection = dbContext.ModuleConnections.Find(dto.Id);

            if (connection == null)
            {
                return false;
            }

            connection.SourceModuleId = dto.SourceId;
            connection.TargetModuleId = dto.TargetId;

            return dbContext.SaveChanges() > 0;
        }

        private static ModuleConnection GetModuleConnection(ModuleConnectionDto dto)
        {
            if (dto == null)
            {
                return null;
            }

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
            if (mc == null)
            {
                return null;
            }

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