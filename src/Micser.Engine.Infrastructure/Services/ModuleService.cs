using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Common.Widgets;
using Micser.Engine.Infrastructure.DataAccess.Models;
using Micser.Engine.Infrastructure.DataAccess.Repositories;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Infrastructure.Services
{
    public class ModuleService : IModuleService
    {
        private readonly IUnitOfWorkFactory _uowFactory;

        public ModuleService(IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
        }

        public ModuleDto Delete(long id)
        {
            using (var uow = _uowFactory.Create())
            {
                var modules = uow.GetRepository<IModuleRepository>();

                var module = modules.Get(id);
                if (module == null)
                {
                    return null;
                }

                modules.Remove(module);
                uow.Complete();

                return GetModuleDto(module);
            }
        }

        public IEnumerable<ModuleDto> GetAll()
        {
            using (var uow = _uowFactory.Create())
            {
                return uow.GetRepository<IModuleRepository>().GetAll().Select(GetModuleDto);
            }
        }

        public ModuleDto GetById(long id)
        {
            using (var uow = _uowFactory.Create())
            {
                var module = uow.GetRepository<IModuleRepository>().Get(id);
                return GetModuleDto(module);
            }
        }

        public bool Insert(ModuleDto moduleDto)
        {
            var module = GetModule(moduleDto);

            using (var uow = _uowFactory.Create())
            {
                var modules = uow.GetRepository<IModuleRepository>();
                modules.Add(module);

                var count = uow.Complete();

                if (count > 0)
                {
                    moduleDto.Id = module.Id;
                    return true;
                }
            }

            return false;
        }

        public bool Update(ModuleDto moduleDto)
        {
            using (var uow = _uowFactory.Create())
            {
                var modules = uow.GetRepository<IModuleRepository>();
                var module = modules.Get(moduleDto.Id);

                if (module == null)
                {
                    return false;
                }

                module.ModuleStateJson = JsonConvert.SerializeObject(moduleDto.ModuleState);
                module.WidgetStateJson = JsonConvert.SerializeObject(moduleDto.WidgetState);

                return uow.Complete() > 0;
            }
        }

        private static Module GetModule(ModuleDto moduleDto)
        {
            if (moduleDto == null)
            {
                return null;
            }

            return new Module
            {
                ModuleType = moduleDto.ModuleType,
                WidgetType = moduleDto.WidgetType,
                ModuleStateJson = JsonConvert.SerializeObject(moduleDto.ModuleState),
                WidgetStateJson = JsonConvert.SerializeObject(moduleDto.WidgetState)
            };
        }

        private static ModuleDto GetModuleDto(Module module)
        {
            if (module == null)
            {
                return null;
            }

            return new ModuleDto
            {
                Id = module.Id,
                ModuleState = JsonConvert.DeserializeObject<ModuleState>(module.ModuleStateJson),
                ModuleType = module.ModuleType,
                WidgetState = JsonConvert.DeserializeObject<WidgetState>(module.WidgetStateJson),
                WidgetType = module.WidgetType
            };
        }
    }
}