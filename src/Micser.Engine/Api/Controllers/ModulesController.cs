using System;
using System.Linq;
using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Nancy;
using Nancy.ModelBinding;

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
            Post["/"] = _ => PostModule();
            Put["/{id:guid}"] = p => PutModule(p.id);
        }

        private dynamic PostModule()
        {
            var module = this.Bind<ModuleDescription>();

            if (module == null || string.IsNullOrEmpty(module.Type))
            {
                return HttpStatusCode.UnprocessableEntity;
            }

            module.Id = Guid.NewGuid();

            _audioEngine.AddModule(module);

            return module;
        }

        private dynamic PutModule(Guid id)
        {
            using (var ctx = _database.GetContext())
            {
                var descriptions = ctx.GetCollection<ModuleDescription>();
                var module = descriptions.GetById(id);

                if (module == null)
                {
                    return HttpStatusCode.NotFound;
                }

                var model = this.Bind<ModuleDescription>();
                module.State = model.State;
                module.ViewState = model.ViewState;

                descriptions.Update(module);
                ctx.Save();

                _audioEngine.UpdateModule(module);
                return module;
            }
        }

        private dynamic GetAll()
        {
            var modules = _audioEngine.Modules;
            return modules.Select(m => m.Description).ToArray();
        }
    }
}