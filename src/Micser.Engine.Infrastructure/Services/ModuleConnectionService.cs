using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.DataAccess.Models;
using Micser.Engine.Infrastructure.DataAccess.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Infrastructure.Services
{
    public class ModuleConnectionService : IModuleConnectionService
    {
        private readonly IUnitOfWorkFactory _uowFactory;

        public ModuleConnectionService(IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
        }

        public ModuleConnectionDto Delete(long id)
        {
            using (var uow = _uowFactory.Create())
            {
                var connections = uow.GetRepository<IModuleConnectionRepository>();
                var connection = connections.Get(id);

                if (connection == null)
                {
                    return null;
                }

                connections.Remove(connection);
                uow.Complete();

                return GetModuleConnectionDto(connection);
            }
        }

        public IEnumerable<ModuleConnectionDto> GetAll()
        {
            using (var uow = _uowFactory.Create())
            {
                return uow.GetRepository<IModuleConnectionRepository>().GetAll().Select(GetModuleConnectionDto);
            }
        }

        public ModuleConnectionDto GetById(long id)
        {
            using (var uow = _uowFactory.Create())
            {
                var connection = uow.GetRepository<IModuleConnectionRepository>().Get(id);
                return GetModuleConnectionDto(connection);
            }
        }

        public bool Insert(ModuleConnectionDto dto)
        {
            using (var uow = _uowFactory.Create())
            {
                var connections = uow.GetRepository<IModuleConnectionRepository>();
                var connection = connections.GetBySourceAndTargetIds(dto.SourceId, dto.TargetId);

                if (connection != null)
                {
                    return false;
                }

                connection = GetModuleConnection(dto);
                connections.Add(connection);

                if (uow.Complete() > 0)
                {
                    dto.Id = connection.Id;
                    return true;
                }
            }

            return false;
        }

        public bool Truncate()
        {
            using (var uow = _uowFactory.Create())
            {
                var repository = uow.GetRepository<IModuleConnectionRepository>();
                var connections = repository.GetAll();
                repository.RemoveRange(connections);
                return uow.Complete() >= 0;
            }
        }

        public bool Update(ModuleConnectionDto dto)
        {
            using (var uow = _uowFactory.Create())
            {
                var connections = uow.GetRepository<IModuleConnectionRepository>();
                var connection = connections.Get(dto.Id);

                if (connection == null)
                {
                    return false;
                }

                connection.SourceModuleId = dto.SourceId;
                connection.TargetModuleId = dto.TargetId;

                return uow.Complete() > 0;
            }
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