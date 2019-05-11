using Newtonsoft.Json;

namespace Micser.Common.Modules
{
    /// <summary>
    /// Contains a module's state variables.
    /// </summary>
    [JsonObject]
    public sealed class ModuleState
    {
        /// <summary>
        /// Creates an instance of the <see cref="ModuleState"/> class.
        /// </summary>
        public ModuleState()
        {
            Data = new StateDictionary();

            Volume = 1f;
        }

        /// <summary>
        /// Gets or sets a state dictionary containing additional non-typed data.
        /// </summary>
        [JsonExtensionData]
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public StateDictionary Data { get; set; }

        /// <summary>
        /// Gets or sets a value whether the module is muted.
        /// </summary>
        public bool IsMuted { get; set; }

        /// <summary>
        /// Gets or sets a value whether the module uses the current system output volume.
        /// </summary>
        public bool UseSystemVolume { get; set; }

        /// <summary>
        /// Gets or sets the module's volume.
        /// </summary>
        public float Volume { get; set; }
    }
}