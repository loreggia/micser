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

        private static ModuleConnectionDto GetModuleConnectionDto(ModuleConnection mc)
        {
            if (mc == null)
            {
                return null;
            }

            return new ModuleConnectionDto(mc.SourceModuleId, mc.TargetModuleId);
        }
    }
}