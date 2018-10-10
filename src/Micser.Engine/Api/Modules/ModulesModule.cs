using Micser.Engine.DataAccess;
using Micser.Infrastructure.Audio;
using Micser.Infrastructure.Models;
using System.Linq;

namespace Micser.Engine.Api.Modules
{
    public class ModulesModule : BaseModule
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IDatabase _database;

        public ModulesModule(IDatabase database, IAudioEngine audioEngine)
            : base("modules")
        {
            _database = database;
            _audioEngine = audioEngine;

            Get["/"] = _ => GetAll();
        }

        private Module[] GetAll()
        {
            using (var db = _database.GetContext())
            {
                return db.GetCollection<Module>().FindAll().ToArray();
            }
        }
    }
}