using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Nancy;
using Nancy.ModelBinding;
using System;
using System.Linq;

namespace Micser.Engine.Api.Controllers
{
    public class ModulesController : ApiController
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IDatabase _database;

        public ModulesController(IAudioEngine audioEngine, IDatabase database)
            : base("modules")
        {
            _audioEngine = audioEngine;
            _database = database;

            Get["/"] = _ => GetAll();
            Post["/"] = _ => CreateModule();
        }

        private dynamic CreateModule()
        {
            var module = this.Bind<ModuleDescription>();

            if (module == null || string.IsNullOrEmpty(module.Type))
            {
                return HttpStatusCode.BadRequest;
            }

            module.Id = Guid.NewGuid();

            _audioEngine.AddModule(module);

            return module;
        }

        private dynamic GetAll()
        {
            var modules = _audioEngine.Modules;
            return modules.Select(m => m.Description).ToArray();
        }
    }
}