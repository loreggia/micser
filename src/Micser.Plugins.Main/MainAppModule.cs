using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Themes;
using Micser.Plugins.Main.Properties;
using Micser.Plugins.Main.Widgets;
using Prism.Ioc;
using System;
using System.Windows;

namespace Micser.Plugins.Main
{
    public class MainAppModule : IPlugin
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var resourceRegistry = containerProvider.Resolve<IResourceRegistry>();
            resourceRegistry.Add(new ResourceDictionary { Source = new Uri("Micser.Plugins.Main;component/Themes/Generic.xaml", UriKind.Relative) });
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterWidget<DeviceInputWidget, DeviceInputViewModel>(Resources.DeviceInputWidgetName);
            containerRegistry.RegisterWidget<DeviceOutputWidget, DeviceOutputViewModel>(Resources.DeviceOutputWidgetName);
        }
    }
}