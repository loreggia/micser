using System.Collections.Generic;
using System.Linq;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Models;

namespace Micser.Engine.Infrastructure.Services
{
    /// <inheritdoc cref="IModuleConnectionService"/>
    public class ModuleConnectionService : IModuleConnectionService
    {
        private readonly EngineDbContext _dbContext;

        /// <inheritdoc />
        public ModuleConnectionService(EngineDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc />
        public ModuleConnectionDto Delete(long id)
        {
            var connection = _dbContext.ModuleConnections.Find(id);

            if (connection == null)
            {
                return null;
            }

            _dbContext.ModuleConnections.Remove(connection);
            _dbContext.SaveChanges();

            return GetModuleConnectionDto(connection);
        }

        /// <inheritdoc />
        public IEnumerable<ModuleConnectionDto> GetAll()
        {
            return _dbContext
                .ModuleConnections
                .AsEnumerable()
                .Select(GetModuleConnectionDto)
                .ToArray();
        }

        /// <inheritdoc />
        public ModuleConnectionDto GetById(long id)
        {
            var connection = _dbContext.ModuleConnections.Find(id);
            return GetModuleConnectionDto(connection);
        }

        /// <inheritdoc />
        public bool Insert(ModuleConnectionDto dto)
        {
            var connection = _dbContext
                .ModuleConnections
                .SingleOrDefault(c => c.SourceModuleId == dto.SourceId && c.TargetModuleId == dto.TargetId);

            if (connection != null)
            {
                return false;
            }

            connection = GetModuleConnection(dto);
            _dbContext.ModuleConnections.Add(connection);

            if (_dbContext.SaveChanges() > 0)
            {
                dto.Id = connection.Id;
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool Truncate()
        {
            var connections = _dbContext.ModuleConnections.AsEnumerable();
            _dbContext.ModuleConnections.RemoveRange(connections);
            return _dbContext.SaveChanges() >= 0;
        }

        /// <inheritdoc />
        public bool Update(ModuleConnectionDto dto)
        {
            var connection = _dbContext.ModuleConnections.Find(dto.Id);

            if (connection == null)
            {
                return false;
            }

            connection.SourceModuleId = dto.SourceId;
            connection.TargetModuleId = dto.TargetId;

            return _dbContext.SaveChanges() > 0;
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