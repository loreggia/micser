using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.DataAccess.Repositories;
using System.Collections.Generic;
using System.Linq;

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

        private IEnumerable<ModuleConnectionDto> GetAll()
        {
            using (var uow = _database.Create())
            {
                var connections = uow.GetRepository<IModuleConnectionRepository>();
                return connections.GetAll().Select(c => new ModuleConnectionDto(c.SourceModuleId, c.TargetModuleId));
            }
        }
    }
}