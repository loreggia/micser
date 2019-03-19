using Newtonsoft.Json;

namespace Micser.Common.Modules
{
    public sealed class ModuleState
    {
        public ModuleState()
        {
            Data = new StateDictionary();

            Volume = 1f;
        }

        [JsonExtensionData]
        public StateDictionary Data { get; set; }

        public bool IsMuted { get; set; }
        public float Volume { get; set; }
    }
}