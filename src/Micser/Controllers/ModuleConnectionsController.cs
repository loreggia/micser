using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Micser.Common.Controllers;
using Micser.Common.Modules;
using Micser.Common.Services;

namespace Micser.Controllers
{
    public class ModuleConnectionsController : ApiController
    {
        private readonly IModuleConnectionService _moduleConnectionService;

        public ModuleConnectionsController(IModuleConnectionService moduleConnectionService)
        {
            _moduleConnectionService = moduleConnectionService;
        }

        [HttpGet("")]
        public IAsyncEnumerable<ModuleConnection> GetAll()
        {
            return _moduleConnectionService.GetAllAsync();
        }
    }
}