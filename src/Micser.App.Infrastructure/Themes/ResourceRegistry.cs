using Micser.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Micser.App.Infrastructure.Themes
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

        public static IEnumerable<ResourceDictionary> InfrastructureResources =>
            _infrastructureResources ?? (_infrastructureResources = new[]
            {
                "Generic.xaml",
                "Icons.xaml",
                "BusyPanel.xaml",
                "Connection.xaml",
                "Connector.xaml",
                "EditableTextBlock.xaml",
                "Expander.xaml",
                "Menu.xaml",
                "SettingsPanel.xaml",
                "Thumbs.xaml",
                "ToolBar.xaml",
                "View.xaml",
                "Widget.xaml",
                "WidgetPanel.xaml",
                "WidgetToolboxItem.xaml",
                "WidgetToolbox.xaml"
            }.Select(x => new ResourceDictionary
            {
                Source = new Uri("Micser.App.Infrastructure;component/Themes/" + x, UriKind.Relative)
            }));

        public static void RegisterResourcesFor(FrameworkElement element)
        {
            element.Resources.MergedDictionaries.AddRange(InfrastructureResources);
        }
    }
}