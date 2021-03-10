using System;

namespace Micser.Common.Modules
{
    /// <summary>
    /// Helper class for module registrations.
    /// </summary>
    public class ModuleDefinition
    {
        /// <summary>
        /// Creates an instance of the <see cref="ModuleDefinition"/> class.
        /// </summary>
        /// <param name="name">The module name.</param>
        /// <param name="type">The module type.</param>
        public ModuleDefinition(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Gets the module name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the module type.
        /// </summary>
        public Type Type { get; }
    }
}