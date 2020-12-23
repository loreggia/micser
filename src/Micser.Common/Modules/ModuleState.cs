using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Micser.Common.Extensions;

namespace Micser.Common.Modules
{
    /// <summary>
    /// Contains a module's state variables.
    /// </summary>
    public sealed class ModuleState : Dictionary<string, string?>
    {
        /// <inheritdoc />
        public ModuleState()
        {
            IsEnabled = true;
            IsMuted = false;
            Volume = 1f;
        }

        /// <summary>
        /// Gets or sets a value whether the module is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => GetPropertyValue<bool>();
            set => this[nameof(IsEnabled)] = value.ToType<string>();
        }

        /// <summary>
        /// Gets or sets a value whether the module is muted.
        /// </summary>
        public bool IsMuted
        {
            get => GetPropertyValue<bool>();
            set => SetPropertyValue(value);
        }

        /// <summary>
        /// Gets or sets the left UI position.
        /// </summary>
        public float Left
        {
            get => GetPropertyValue<float>();
            set => SetPropertyValue(value);
        }

        /// <summary>
        /// Gets or sets the top UI position.
        /// </summary>
        public float Top
        {
            get => GetPropertyValue<float>();
            set => SetPropertyValue(value);
        }

        /// <summary>
        /// Gets or sets a value whether the module uses the current system output volume.
        /// </summary>
        public bool UseSystemVolume
        {
            get => GetPropertyValue<bool>();
            set => SetPropertyValue(value);
        }

        /// <summary>
        /// Gets or sets the module's volume.
        /// </summary>
        public float Volume
        {
            get => GetPropertyValue<float>();
            set => SetPropertyValue(value);
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

        private T? GetPropertyValue<T>([CallerMemberName] string propertyName = null!)
        {
            return TryGetValue(propertyName, out var value) && value != null ? value.ToType<T>() : default;
        }

        private void SetPropertyValue<T>(T value, [CallerMemberName] string propertyName = null!)
        {
            this[propertyName] = value?.ToType<string>();
        }
    }
}