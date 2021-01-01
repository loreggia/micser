using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Micser.Common.Controllers;
using Micser.Common.Modules;
using Micser.Common.Services;

namespace Micser.Controllers
{
    public class ModulesController : ApiController
    {
        private readonly IEnumerable<ModuleDescription> _moduleDescriptions;
        private readonly IModuleService _moduleService;

        public ModulesController(IModuleService moduleService, IEnumerable<ModuleDescription> moduleDescriptions)
        {
            _moduleService = moduleService;
            _moduleDescriptions = moduleDescriptions;
        }

        [HttpPost("")]
        public async Task<Module> CreateAsync([FromBody] Module module)
        {
            await _moduleService.InsertAsync(module);
            return module;
        }

        [HttpGet("")]
        public IAsyncEnumerable<Module> GetAllAsync()
        {
            return _moduleService.GetAllAsync();
        }

        [HttpGet("Descriptions")]
        public IEnumerable<ModuleDescription> GetDescriptions()
        {
            return _moduleDescriptions;
        }
    }
}