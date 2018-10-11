using Micser.Infrastructure.Extensions;
using Micser.Infrastructure.Themes;
using Micser.Plugins.Main.Properties;
using Micser.Plugins.Main.Views.Widgets;
using Micser.Plugins.Main.Widgets;
using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Windows;

namespace Micser.Plugins.Main
{
    public class MainAppModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var resourceRegistry = containerProvider.Resolve<IResourceRegistry>();
            resourceRegistry.Add(new ResourceDictionary { Source = new Uri("Micser.Main;component/Themes/Generic.xaml", UriKind.Relative) });
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterWidget<DeviceInputWidget, DeviceInputViewModel>(Resources.DeviceInputWidgetName);
            containerRegistry.RegisterWidget<DeviceOutputWidget, DeviceOutputViewModel>(Resources.DeviceOutputWidgetName);
        }
    }
}