using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Micser.Infrastructure.Themes
{
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

        public static IEnumerable<ResourceDictionary> InfrastructureResources => _infrastructureResources ?? (_infrastructureResources = new[]
                {
            new ResourceDictionary{Source = new Uri("Micser.Infrastructure;component/Themes/Generic.xaml", UriKind.Relative)},
            new ResourceDictionary{Source = new Uri("Micser.Infrastructure;component/Themes/Connection.xaml",UriKind.Relative)},
            new ResourceDictionary{Source = new Uri("Micser.Infrastructure;component/Themes/Connector.xaml",UriKind.Relative)},
            new ResourceDictionary{Source = new Uri("Micser.Infrastructure;component/Themes/Thumbs.xaml",UriKind.Relative)},
            new ResourceDictionary{Source = new Uri("Micser.Infrastructure;component/Themes/WidgetPanel.xaml",UriKind.Relative)},
            new ResourceDictionary{Source = new Uri("Micser.Infrastructure;component/Themes/Widget.xaml",UriKind.Relative)}
        });

        public static void RegisterResourcesFor(FrameworkElement element)
        {
            element.Resources.MergedDictionaries.AddRange(InfrastructureResources);
        }
    }
}