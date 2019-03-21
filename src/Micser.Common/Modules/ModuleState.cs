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
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public StateDictionary Data { get; set; }

        public bool IsMuted { get; set; }
        public bool UseSystemVolume { get; set; }
        public float Volume { get; set; }
    }
}