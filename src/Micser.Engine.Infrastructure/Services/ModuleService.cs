using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.DataAccess.Models;
using Micser.Engine.Infrastructure.DataAccess.Repositories;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Infrastructure.Services
{
    /// <inheritdoc cref="IModuleService"/>
    public class ModuleService : IModuleService
    {
        private readonly IUnitOfWorkFactory _uowFactory;

        /// <inheritdoc />
        public ModuleService(IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IEnumerable<ModuleDto> GetAll()
        {
            using (var uow = _uowFactory.Create())
            {
                return uow.GetRepository<IModuleRepository>().GetAll().Select(GetModuleDto);
            }
        }

        /// <inheritdoc />
        public ModuleDto GetById(long id)
        {
            using (var uow = _uowFactory.Create())
            {
                var module = uow.GetRepository<IModuleRepository>().Get(id);
                return GetModuleDto(module);
            }
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool Truncate()
        {
            using (var uow = _uowFactory.Create())
            {
                var repository = uow.GetRepository<IModuleRepository>();
                var modules = repository.GetAll();
                repository.RemoveRange(modules);
                return uow.Complete() >= 0;
            }
        }

        /// <inheritdoc />
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

                module.StateJson = JsonConvert.SerializeObject(moduleDto.State);

                return uow.Complete() >= 0;
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
                StateJson = JsonConvert.SerializeObject(moduleDto.State)
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
                ModuleType = module.ModuleType,
                WidgetType = module.WidgetType,
                State = JsonConvert.DeserializeObject<ModuleState>(module.StateJson)
            };
        }
    }
}