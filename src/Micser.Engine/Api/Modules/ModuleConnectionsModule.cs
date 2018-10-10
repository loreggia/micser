using Micser.Engine.DataAccess;
using Micser.Infrastructure.Audio;
using Micser.Infrastructure.Models;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Api.Modules
{
    public class ModuleConnectionsModule : BaseModule
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IDatabase _database;

        public ModuleConnectionsModule(IDatabase database, IAudioEngine audioEngine)
            : base("moduleconnections")
        {
            _database = database;
            _audioEngine = audioEngine;
            Get["/"] = _ => GetAll();
        }

        private IEnumerable<ModuleConnection> GetAll()
        {
            using (var db = _database.GetContext())
            {
                return db.GetCollection<ModuleConnection>().FindAll().ToArray();
            }
        }
    }
}