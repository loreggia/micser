using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.Services;
using Nancy;
using Nancy.ModelBinding;
using System.Collections.Generic;

namespace Micser.Engine.Api.Controllers
{
    public class ModuleConnectionsController : ApiController
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IModuleConnectionService _moduleConnectionService;

        public ModuleConnectionsController(IAudioEngine audioEngine, IModuleConnectionService moduleConnectionService)
            : base("moduleconnections")
        {
            _audioEngine = audioEngine;
            _moduleConnectionService = moduleConnectionService;
            Get["/"] = _ => GetAll();
            Post["/"] = _ => Insert();
        }

        private IEnumerable<ModuleConnectionDto> GetAll()
        {
            return _moduleConnectionService.GetAll();
        }

        private dynamic Insert()
        {
            var dto = this.Bind<ModuleConnectionDto>();

            if (dto.SourceId <= 0 || dto.TargetId <= 0)
            {
                return HttpStatusCode.UnprocessableEntity;
            }

            if (!_moduleConnectionService.Insert(dto))
            {
                return HttpStatusCode.InternalServerError;
            }

            return dto;
        }
    }
}