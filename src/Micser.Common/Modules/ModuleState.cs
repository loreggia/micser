using Newtonsoft.Json;
using ProtoBuf;
using System.Collections.Generic;

namespace Micser.Common.Modules
{
    /// <summary>
    /// Contains a module's state variables.
    /// </summary>
    [JsonObject]
    [ProtoContract]
    public sealed class ModuleState
    {
        /// <inheritdoc />
        public ModuleState()
        {
            Data = new Dictionary<string, object>();
            IsEnabled = true;
            Volume = 1f;
        }

        /// <summary>
        /// Gets or sets a state dictionary containing additional non-typed data.
        /// </summary>
        [JsonExtensionData]
        [ProtoMember(5)]
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public Dictionary<string, object> Data { get; set; }

        /// <summary>
        /// Gets or sets a value whether the module is enabled.
        /// </summary>
        [ProtoMember(1)]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value whether the module is muted.
        /// </summary>
        [ProtoMember(3)]
        public bool IsMuted { get; set; }

        /// <summary>
        /// Gets or sets a value whether the module uses the current system output volume.
        /// </summary>
        [ProtoMember(4)]
        public bool UseSystemVolume { get; set; }

        /// <summary>
        /// Gets or sets the module's volume.
        /// </summary>
        [ProtoMember(2)]
        public float Volume { get; set; }
    }
}