using Micser.Common.DataAccess;
using System.Collections.Generic;

namespace Micser.Engine.Infrastructure.DataAccess.Models
{
    public class Module : Model
    {
        public string ModuleStateJson { get; set; }
        public string ModuleType { get; set; }
        public virtual ICollection<ModuleConnection> SourceModuleConnections { get; set; }
        public virtual ICollection<ModuleConnection> TargetModuleConnections { get; set; }
        public string WidgetStateJson { get; set; }
        public string WidgetType { get; set; }
    }
}