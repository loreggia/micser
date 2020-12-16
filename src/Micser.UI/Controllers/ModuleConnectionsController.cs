using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Micser.Common.Modules;
using Micser.Common.Services;

namespace Micser.UI.Controllers
{
    public class ModuleConnectionsController : ApiController
    {
        private readonly IModuleConnectionService _moduleConnectionService;

        public ModuleConnectionsController(IModuleConnectionService moduleConnectionService)
        {
            _moduleConnectionService = moduleConnectionService;
        }

        [HttpGet("")]
        public IEnumerable<ModuleConnectionDto> GetAll()
        {
            return _moduleConnectionService.GetAll();
        }
    }
}