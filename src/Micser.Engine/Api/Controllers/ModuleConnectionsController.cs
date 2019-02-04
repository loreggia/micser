using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.Services;
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
        }

        private IEnumerable<ModuleConnectionDto> GetAll()
        {
            return _moduleConnectionService.GetAll();
        }
    }
}