using System.Collections.Generic;

namespace Micser.Common.Modules
{
    public class ModuleState
    {
        public ModuleState()
        {
            Data = new Dictionary<string, object>();
        }

        public Dictionary<string, object> Data { get; set; }
    }
}