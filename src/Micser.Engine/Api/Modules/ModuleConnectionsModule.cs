using Micser.Engine.DataAccess;
using System.Collections.Generic;
using System.Linq;
using Micser.Infrastructure.Audio;
using Micser.Infrastructure.Models;

namespace Micser.Engine.Api.Modules
{
    public class ModuleConnectionsModule : BaseModule
    {
        private readonly IAudioEngine _audioEngine;

        public ModuleConnectionsModule(IAudioEngine audioEngine)
            : base("moduleconnections")
        {
            _audioEngine = audioEngine;
            Get["/"] = _ => GetAll();
        }

        private IEnumerable<ModuleConnection> GetAll()
        {
            using (var db = new Database())
            {
                return db.GetCollection<ModuleConnection>().FindAll().ToArray();
            }
        }
    }
}