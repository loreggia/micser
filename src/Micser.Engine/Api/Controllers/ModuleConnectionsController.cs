using Micser.Engine.DataAccess;
using Micser.Infrastructure.Audio;
using Micser.Infrastructure.Models;
using System.Collections.Generic;
using System.Linq;

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
            using (var db = _database.GetContext())
            {
                return db.GetCollection<ModuleConnectionDescription>().FindAll().ToArray();
            }
        }
    }
}