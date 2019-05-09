using Micser.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Micser.App.Infrastructure.Themes
{
    /// <inheritdoc cref="IResourceRegistry"/>
    public class ResourceRegistry : ItemRegistry<ResourceDictionary>, IResourceRegistry
    {
        private static IEnumerable<ResourceDictionary> _infrastructureResources;

        public ResourceRegistry()
        {
            foreach (var infrastructureResource in InfrastructureResources)
            {
                Add(infrastructureResource);
            }
        }

        public static IEnumerable<ResourceDictionary> InfrastructureResources =>
            _infrastructureResources ?? (_infrastructureResources = new[]
            {
                new ResourceDictionary {Source = new Uri("Micser.App.Infrastructure;component/Themes/Generic.xaml", UriKind.Relative)}
            });

        public static void RegisterResourcesFor(FrameworkElement element)
        {
            element.Resources.MergedDictionaries.AddRange(InfrastructureResources);
        }
    }
}