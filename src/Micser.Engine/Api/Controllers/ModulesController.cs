﻿using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using System.Linq;

namespace Micser.Engine.Api.Controllers
{
    public class ModulesController : ApiController
    {
        private readonly IAudioEngine _audioEngine;

        public ModulesController(IAudioEngine audioEngine)
            : base("modules")
        {
            _audioEngine = audioEngine;

            Get["/"] = _ => GetAll();
        }

        private ModuleDescription[] GetAll()
        {
            var modules = _audioEngine.Modules;
            return modules.Select(m => m.Description).ToArray();
        }
    }
}