using System;

namespace Micser.Common.Api
{
    /// <summary>
    /// Specifies the name of a request processor for automatic registration.
    /// </summary>
    public class RequestProcessorNameAttribute : Attribute
    {
        /// <summary>
        /// The name of the default request processor that is used when a request did not specify a resource name.
        /// </summary>
        public const string DefaultName = "<default>";

        /// <summary>
        /// Creates an instance of the <see cref="RequestProcessorNameAttribute"/> class with the specified name.
        /// </summary>
        /// <param name="name">The (resource) name of the request processor.</param>
        public RequestProcessorNameAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the associated request processor.
        /// </summary>
        public string Name { get; }
    }
}