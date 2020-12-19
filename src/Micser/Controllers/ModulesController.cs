using System.Collections.Generic;
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

        [HttpGet("")]
        public IEnumerable<ModuleDto> GetAll()
        {
            return _moduleService.GetAll();
        }
    }
}