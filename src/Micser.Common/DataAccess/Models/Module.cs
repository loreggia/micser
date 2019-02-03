using System.Collections.Generic;

namespace Micser.Common.DataAccess.Models
{
    public class Module : Model
    {
        public virtual ICollection<ModuleConnection> FromModuleConnections { get; set; }
        public string ModuleStateJson { get; set; }
        public string ModuleType { get; set; }
        public virtual ICollection<ModuleConnection> ToModuleConnections { get; set; }
        public string WidgetStateJson { get; set; }
        public string WidgetType { get; set; }
    }
}