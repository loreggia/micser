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

        public IEnumerable<ModuleConnectionDto> GetAll()
        {
            using (var uow = _uowFactory.Create())
            {
                return uow.GetRepository<IModuleConnectionRepository>().GetAll().Select(GetModuleConnectionDto);
            }
        }

        public bool Insert(ModuleConnectionDto dto)
        {
            using (var uow = _uowFactory.Create())
            {
                var connections = uow.GetRepository<IModuleConnectionRepository>();
                var existing = connections.GetBySourceAndTargetIds(dto.SourceId, dto.TargetId);

                if (existing != null)
                {
                    dto.Id = existing.Id;
                    return true;
                }
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
                SourceModuleId = dto.SourceId,
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
                SourceId = mc.SourceModuleId,
                TargetId = mc.TargetModuleId
            };
        }
    }
}