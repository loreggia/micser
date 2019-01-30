using Newtonsoft.Json;

namespace Micser.Common.Modules
{
    public sealed class ModuleState
    {
        public ModuleState()
        {
            Data = new StateDictionary();
        }

        [JsonExtensionData]
        public StateDictionary Data { get; set; }
    }
}