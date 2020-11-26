using System.Collections.Generic;
using Micser.Common.Extensions;

namespace Micser.Common.Modules
{
    /// <summary>
    /// Contains a module's state variables.
    /// </summary>
    public sealed class ModuleState : Dictionary<string, string>
    {
        /// <inheritdoc />
        public ModuleState()
        {
            IsEnabled = true;
            Volume = 1f;
        }

        /// <summary>
        /// Gets or sets a value whether the module is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => !TryGetValue(nameof(IsEnabled), out var value) || value.ToType<bool>();
            set => this[nameof(IsEnabled)] = value.ToType<string>();
        }

        /// <summary>
        /// Gets or sets a value whether the module is muted.
        /// </summary>
        public bool IsMuted
        {
            get => !TryGetValue(nameof(IsMuted), out var value) || value.ToType<bool>();
            set => this[nameof(IsMuted)] = value.ToType<string>();
        }

        /// <summary>
        /// Gets or sets a value whether the module uses the current system output volume.
        /// </summary>
        public bool UseSystemVolume
        {
            get => !TryGetValue(nameof(UseSystemVolume), out var value) || value.ToType<bool>();
            set => this[nameof(UseSystemVolume)] = value.ToType<string>();
        }

        /// <summary>
        /// Gets or sets the module's volume.
        /// </summary>
        public float Volume
        {
            get => TryGetValue(nameof(Volume), out var value) ? value.ToType<float>() : default;
            set => this[nameof(Volume)] = value.ToType<string>();
        }

        /// <summary>
        /// Adds the specified entries to the state. The keys and values are not automatically cloned.
        /// </summary>
        /// <param name="entries">The entries to add to the state.</param>
        public void Add(IDictionary<string, string> entries)
        {
            foreach (var (key, value) in entries)
            {
                Add(key, value);
            }
        }
    }
}