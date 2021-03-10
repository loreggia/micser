using System;
using System.Diagnostics.CodeAnalysis;

namespace Micser.Common
{
    /// <summary>
    /// Represents an error for a missing application configuration.
    /// </summary>
    [SuppressMessage("Design", "RCS1194:Implement exception constructors.")]
    public class MissingConfigurationException : Exception
    {
        /// <summary>
        /// Creates a new instance of the <see cref="MissingConfigurationException"/> class.
        /// </summary>
        /// <param name="section">The missing or affected configuration section.</param>
        public MissingConfigurationException(string section)
        {
            Section = section;
        }

        /// <summary>
        /// Gets the affected configuration section.
        /// </summary>
        public string Section { get; }
    }
}