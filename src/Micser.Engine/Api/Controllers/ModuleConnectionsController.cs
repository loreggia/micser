using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using System.Collections.Generic;

namespace Micser.Engine.Api.Controllers
{
    public class ModuleConnectionsController : ApiController
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IUnitOfWorkFactory _database;

        public ModuleConnectionsController(IUnitOfWorkFactory database, IAudioEngine audioEngine)
            : base("moduleconnections")
        {
            _database = database;
            _audioEngine = audioEngine;
            Get["/"] = _ => GetAll();
        }

        private IEnumerable<ModuleConnectionDescription> GetAll()
        {
            using (var uow = _database.Create())
            {
                return uow.ModuleConnections.GetAll();
            }
        }
    }
}