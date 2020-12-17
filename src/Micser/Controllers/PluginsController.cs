using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Micser.Common;

namespace Micser.UI.Controllers
{
    public class PluginsController : ApiController
    {
        private readonly IEnumerable<IPlugin> _plugins;

        public PluginsController(IEnumerable<IPlugin> plugins)
        {
            _plugins = plugins;
        }

        [Route("")]
        public IEnumerable<string> GetPluginNames()
        {
            return _plugins
                .Where(p => p.HasUI)
                .Select(p => p.GetType().Assembly.GetName().Name ?? throw new InvalidOperationException("Plugin assembly could not be determined."))
                .ToArray();
        }
    }
}