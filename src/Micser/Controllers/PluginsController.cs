using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Micser.Common;
using Micser.Common.Controllers;

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
        public IEnumerable<PluginDescription> GetPluginNames()
        {
            return _plugins
                .Where(p => p.UIModuleName != null)
                .Select(p => new PluginDescription
                {
                    AssemblyName = p.GetType().Assembly.GetName().Name ?? throw new InvalidOperationException("Plugin assembly could not be determined."),
                    ModuleName = p.UIModuleName!
                })
                .ToArray();
        }

        public class PluginDescription
        {
            public string AssemblyName { get; init; }
            public string ModuleName { get; init; }
        }
    }
}