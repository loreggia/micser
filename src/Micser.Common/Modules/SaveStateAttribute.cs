using System;

namespace Micser.Common.Modules
{
    /// <summary>
    /// Declares a property to be serialized into the module's state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SaveStateAttribute : Attribute
    {
        /// <summary>
        /// Creates an instance of the <see cref="SaveStateAttribute"/> class with the specified default value.
        /// </summary>
        /// <param name="defaultValue">The value to use if the property is not set.</param>
        public SaveStateAttribute(object defaultValue)
        {
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Gets the property's default value.
        /// </summary>
        public object DefaultValue { get; }
    }
}