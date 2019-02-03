using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Nancy;
using Nancy.ModelBinding;
using System;

namespace Micser.Engine.Api.Controllers
{
    public class ModulesController : ApiController
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IUnitOfWorkFactory _database;

        public ModulesController(IAudioEngine audioEngine, IUnitOfWorkFactory database)
            : base("modules")
        {
            _audioEngine = audioEngine;
            _database = database;

            Get["/"] = _ => GetAll();
            Post["/"] = _ => InsertModule();
            Put["/{id:guid}"] = p => UpdateModule(p.id);
        }

        private dynamic DeleteModule(Guid id)
        {
            using (var uow = _database.Create())
            {
            }
        }

        private dynamic GetAll()
        {
            using (var ctx = _database.Create())
            {
                return ctx.Modules.GetAll();
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

            using (var ctx = _database.Create())
            {
                ctx.GetCollection<ModuleDescription>().Insert(module);
                ctx.Save();
            }

            _audioEngine.AddModule(module);

            return module;
        }

        private dynamic UpdateModule(Guid id)
        {
            using (var ctx = _database.Create())
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