using Micser.Engine.DataAccess;
using Micser.Shared.Models;
using System.Linq;

namespace Micser.Engine.Api.Modules
{
    public class ModuleDescriptionsModule : BaseModule
    {
        public ModuleDescriptionsModule()
            : base("moduledescriptions")
        {
            Get["/"] = _ => GetAll();
        }

        private ModuleDescription[] GetAll()
        {
            using (var db = new Database())
            {
                return db.GetCollection<ModuleDescription>().FindAll().ToArray();
            }
        }
    }
}