using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Micser.Common.Controllers;
using Micser.Common.Modules;

namespace Micser.Controllers
{
    public class WidgetsController : ApiController
    {
        private readonly IEnumerable<ModuleDescription> _moduleDescriptions;

        public WidgetsController(IEnumerable<ModuleDescription> moduleDescriptions)
        {
            _moduleDescriptions = moduleDescriptions;
        }

        [HttpGet("")]
        public IEnumerable<ModuleDescription> GetAll()
        {
            return _moduleDescriptions;
        }
    }
}