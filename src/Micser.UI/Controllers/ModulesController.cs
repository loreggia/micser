using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Micser.Common.Modules;
using Micser.Common.Services;

namespace Micser.UI.Controllers
{
    public class ModulesController : ApiController
    {
        private readonly IModuleService _moduleService;

        public ModulesController(IModuleService moduleService)
        {
            _moduleService = moduleService;
        }

        [HttpGet("")]
        public IEnumerable<ModuleDto> GetAll()
        {
            return _moduleService.GetAll();
        }
    }
}