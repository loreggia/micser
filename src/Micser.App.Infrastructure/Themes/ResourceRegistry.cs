using Micser.Common;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Micser.App.Infrastructure.Themes
{
    /// <inheritdoc cref="IResourceRegistry"/>
    public class ResourceRegistry : ItemRegistry<ResourceDictionary>, IResourceRegistry
    {
        private static IEnumerable<ResourceDictionary> _infrastructureResources;

        /// <summary>
        /// Creates an instance of the <see cref="ResourceRegistry"/> class.
        /// </summary>
        public ResourceRegistry()
        {
            foreach (var infrastructureResource in InfrastructureResources)
            {
                Add(infrastructureResource);
            }
        }

        /// <summary>
        /// Gets all resources required by the infrastructure module.
        /// </summary>
        public static IEnumerable<ResourceDictionary> InfrastructureResources =>
            _infrastructureResources ?? (_infrastructureResources = new[]
            {
                new ResourceDictionary {Source = new Uri("Micser.App.Infrastructure;component/Themes/Generic.xaml", UriKind.Relative)}
            });
    }
}