using Micser.Engine.Audio;
using System.Collections.Generic;
using System.Linq;
using Micser.Infrastructure.DataAccess;
using Micser.Infrastructure.Models;

namespace Micser.Engine.Api.Controllers
{
    public class ModuleConnectionsController : Controller
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