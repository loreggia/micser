using Micser.Engine.DataAccess;
using Micser.Shared.Models;
using System.Linq;

namespace Micser.Engine.Api.Modules
{
    public class AudioModuleDescriptionsModule : BaseModule
    {
        public AudioModuleDescriptionsModule()
            : base("audiomoduledescriptions")
        {
            Get["/"] = _ => GetAll();
        }

        private AudioModuleDescription[] GetAll()
        {
            using (var db = new Database())
            {
                return db.GetCollection<AudioModuleDescription>().FindAll().ToArray();
            }
        }
    }
}