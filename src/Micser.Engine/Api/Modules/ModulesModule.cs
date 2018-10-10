using Micser.Engine.DataAccess;
using System.Linq;
using Micser.Infrastructure.Audio;
using Micser.Infrastructure.Models;

namespace Micser.Engine.Api.Modules
{
    public class ModulesModule : BaseModule
    {
        private readonly IAudioEngine _audioEngine;

        public ModulesModule(IAudioEngine audioEngine)
            : base("modules")
        {
            _audioEngine = audioEngine;
            Get["/"] = _ => GetAll();
        }

        private Module[] GetAll()
        {
            using (var db = new Database())
            {
                return db.GetCollection<Module>().FindAll().ToArray();
            }
        }
    }
}