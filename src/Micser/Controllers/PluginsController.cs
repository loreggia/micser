using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Micser.Common;
using Micser.Common.Controllers;
using Micser.Models;

namespace Micser.Controllers
{
    public class PluginsController : ApiController
    {
        private readonly IEnumerable<IPlugin> _plugins;

        public PluginsController(IEnumerable<IPlugin> plugins)
        {
            _plugins = plugins;
        }

        [Route("")]
        public IEnumerable<PluginDescriptionDto> GetPluginNames()
        {
            return _plugins
                .Where(p => p.UIModuleName != null)
                .Select(p => new PluginDescriptionDto(
                    p.GetType().Assembly.GetName().Name ?? throw new InvalidOperationException("Plugin assembly could not be determined."),
                    p.UIModuleName!)
                )
                .ToArray();
        }
    }
}