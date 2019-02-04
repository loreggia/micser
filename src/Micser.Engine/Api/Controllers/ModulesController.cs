using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Common.Widgets;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.DataAccess.Models;
using Micser.Engine.Infrastructure.DataAccess.Repositories;
using Nancy;
using Nancy.ModelBinding;
using Newtonsoft.Json;
using System.Linq;

namespace Micser.Engine.Api.Controllers
{
    public class ModulesController : ApiController
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IUnitOfWorkFactory _database;

        public ModulesController(IAudioEngine audioEngine, IUnitOfWorkFactory database)
            : base("modules")
        {
            _audioEngine = audioEngine;
            _database = database;

            Get["/"] = _ => GetAll();
            Post["/"] = _ => InsertModule();
            Put["/{id:long}"] = p => UpdateModule(p.id);
            Delete["/{id:long}"] = p => DeleteModule(p.id);
        }

        private static ModuleDto GetModuleDto(Module m)
        {
            return new ModuleDto
            {
                Id = m.Id,
                ModuleState = JsonConvert.DeserializeObject<ModuleState>(m.ModuleStateJson),
                ModuleType = m.ModuleType,
                WidgetState = JsonConvert.DeserializeObject<WidgetState>(m.WidgetStateJson),
                WidgetType = m.WidgetType
            };
        }

        private dynamic DeleteModule(long id)
        {
            using (var uow = _database.Create())
            {
                var modules = uow.GetRepository<IModuleRepository>();
                var module = modules.Get(id);
                if (module == null)
                {
                    return HttpStatusCode.NotFound;
                }

                modules.Remove(module);
                uow.Complete();

                return module;
            }
        }

        private dynamic GetAll()
        {
            using (var uow = _database.Create())
            {
                var modules = uow.GetRepository<IModuleRepository>();
                return modules.GetAll().Select(GetModuleDto);
            }
        }

        private dynamic InsertModule()
        {
            var moduleDto = this.Bind<ModuleDto>();

            if (string.IsNullOrEmpty(moduleDto?.ModuleType) ||
                string.IsNullOrEmpty(moduleDto.WidgetType))
            {
                return HttpStatusCode.UnprocessableEntity;
            }

            var module = new Module
            {
                ModuleType = moduleDto.ModuleType,
                WidgetType = moduleDto.WidgetType,
                ModuleStateJson = JsonConvert.SerializeObject(moduleDto.ModuleState),
                WidgetStateJson = JsonConvert.SerializeObject(moduleDto.WidgetState)
            };

            using (var uow = _database.Create())
            {
                var modules = uow.GetRepository<IModuleRepository>();
                modules.Add(module);

                if (uow.Complete() <= 0)
                {
                    return HttpStatusCode.InternalServerError;
                }
            }

            _audioEngine.AddModule(module.Id);

            return module;
        }

        private dynamic UpdateModule(long id)
        {
            var moduleDto = this.Bind<ModuleDto>();

            using (var uow = _database.Create())
            {
                var modules = uow.GetRepository<IModuleRepository>();
                var module = modules.Get(id);

                if (module == null)
                {
                    return HttpStatusCode.NotFound;
                }

                module.ModuleStateJson = JsonConvert.SerializeObject(moduleDto.ModuleState);
                module.WidgetStateJson = JsonConvert.SerializeObject(moduleDto.WidgetState);

                uow.Complete();

                _audioEngine.UpdateModule(module.Id);

                return GetModuleDto(module);
            }
        }
    }
}