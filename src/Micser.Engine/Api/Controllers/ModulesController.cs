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
            Post["/"] = _ => InsertModule();
            Put["/{id:guid}"] = p => UpdateModule(p.id);
        }

        private dynamic GetAll()
        {
            using (var ctx = _database.GetContext())
            {
                return ctx.GetCollection<ModuleDescription>().ToArray();
            }
        }

        private dynamic InsertModule()
        {
            var module = this.Bind<ModuleDescription>();

            if (string.IsNullOrEmpty(module?.ModuleType) ||
                string.IsNullOrEmpty(module.WidgetType))
            {
                return HttpStatusCode.UnprocessableEntity;
            }

            module.Id = Guid.NewGuid();

            using (var ctx = _database.GetContext())
            {
                ctx.GetCollection<ModuleDescription>().Insert(module);
                ctx.Save();
            }

            _audioEngine.AddModule(module);

            return module;
        }

        private dynamic UpdateModule(Guid id)
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
                module.ModuleState = model.ModuleState;
                module.WidgetState = model.WidgetState;

                descriptions.Update(module);
                ctx.Save();

                _audioEngine.UpdateModule(module);
                return module;
            }
        }
    }
}