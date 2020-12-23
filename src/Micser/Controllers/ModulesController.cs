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
        private readonly IModuleService _moduleService;

        public ModulesController(IModuleService moduleService)
        {
            _moduleService = moduleService;
        }

        [HttpPost("")]
        public async Task<ModuleDto> CreateAsync(ModuleDto module)
        {
            await _moduleService.InsertAsync(module);
            return module;
        }

        [HttpGet("")]
        public IAsyncEnumerable<ModuleDto> GetAllAsync()
        {
            return _moduleService.GetAllAsync();
        }
    }
}