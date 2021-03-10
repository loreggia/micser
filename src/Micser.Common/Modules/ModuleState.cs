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
        /// <summary>
        /// Creates an instance of the <see cref="ModuleState"/> class.
        /// </summary>
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
            set => SetPropertyValue(value);
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
        /// Gets a value for the specified key and tries to convert it to type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value that is returned if the value does not exist or failed to be converted to type <typeparamref name="T"/>.</param>
        /// <typeparam name="T">The result type.</typeparam>
        public T? GetValue<T>(string key, T? defaultValue = default)
        {
            return GetPropertyValue<T>(key) ?? defaultValue;
        }

        private T? GetPropertyValue<T>([CallerMemberName] string propertyName = null!)
        {
            return TryGetValue(propertyName.ToCamelCase(), out var value) && value != null ? value.ToType<T>() : default;
        }

        private void SetPropertyValue<T>(T value, [CallerMemberName] string propertyName = null!)
        {
            this[propertyName.ToCamelCase()] = value?.ToType<string>();
        }
    }
}