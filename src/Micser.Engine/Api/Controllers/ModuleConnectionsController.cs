using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Api.Controllers
{
    public class ModuleConnectionsController : ApiController
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IDatabase _database;

        public ModuleConnectionsController(IDatabase database, IAudioEngine audioEngine)
            : base("moduleconnections")
        {
            _database = database;
            _audioEngine = audioEngine;
            Get["/"] = _ => GetAll();
        }

        private IEnumerable<ModuleConnectionDescription> GetAll()
        {
            var db = _database.GetContext();
            return db.GetCollection<ModuleConnectionDescription>().ToArray();
        }
    }
}