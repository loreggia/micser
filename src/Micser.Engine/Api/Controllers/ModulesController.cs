using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.Services;
using Nancy;
using Nancy.ModelBinding;
using System.Collections.Generic;

namespace Micser.Engine.Api.Controllers
{
    public class ModulesController : ApiController
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IModuleService _moduleService;

        public ModulesController(IAudioEngine audioEngine, IModuleService moduleService)
            : base("modules")
        {
            _audioEngine = audioEngine;
            _moduleService = moduleService;

            Get["/"] = _ => GetAll();
            Post["/"] = _ => InsertModule();
            Put["/{id:long}"] = p => UpdateModule(p.id);
            Delete["/{id:long}"] = p => DeleteModule(p.id);
            Post["/{id:long}/changestatedata"] = p => ChangeStateData(p.id);
        }

        private dynamic ChangeStateData(long id)
        {
            var moduleDto = _moduleService.GetById(id);

            if (moduleDto == null)
            {
                return HttpStatusCode.NotFound;
            }

            var changeDto = this.Bind<ModuleStateChangeDto>();

            if (string.IsNullOrEmpty(changeDto?.Key))
            {
                return HttpStatusCode.UnprocessableEntity;
            }

            moduleDto.WidgetState.Data[changeDto.Key] = changeDto.Value;
            //todo _audioEngine.
            return null;
        }

        private dynamic DeleteModule(long id)
        {
            var module = _moduleService.Delete(id);

            if (module == null)
            {
                return HttpStatusCode.NotFound;
            }

            _audioEngine.DeleteModule(id);

            return module;
        }

        private IEnumerable<ModuleDto> GetAll()
        {
            return _moduleService.GetAll();
        }

        private dynamic InsertModule()
        {
            var moduleDto = this.Bind<ModuleDto>();

            if (string.IsNullOrEmpty(moduleDto?.ModuleType) ||
                string.IsNullOrEmpty(moduleDto.WidgetType))
            {
                return HttpStatusCode.UnprocessableEntity;
            }

            if (!_moduleService.Insert(moduleDto))
            {
                return HttpStatusCode.InternalServerError;
            }

            _audioEngine.AddModule(moduleDto.Id);

            return moduleDto;
        }

        private dynamic UpdateModule(long id)
        {
            var module = _moduleService.GetById(id);

            if (module == null)
            {
                return HttpStatusCode.NotFound;
            }

            var moduleDto = this.Bind<ModuleDto>();

            module.WidgetState = moduleDto.WidgetState;

            if (!_moduleService.Update(module))
            {
                return HttpStatusCode.InternalServerError;
            }

            _audioEngine.UpdateModule(id);

            return moduleDto;
        }
    }
}