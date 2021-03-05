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
        /// Creates an instance of the <see cref="SaveStateAttribute"/> class.
        /// </summary>
        public SaveStateAttribute()
        {
        }
    }
}