using System;
using System.Windows;
using Micser.Infrastructure.Extensions;
using Micser.Infrastructure.Themes;
using Micser.Infrastructure.Widgets;
using Micser.Plugins.Main.Properties;
using Micser.Plugins.Main.ViewModels;
using Micser.Plugins.Main.ViewModels.Widgets;
using Micser.Plugins.Main.Views;
using Micser.Plugins.Main.Views.Widgets;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;

namespace Micser.Plugins.Main
{
    public class MainModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var resourceRegistry = containerProvider.Resolve<IResourceRegistry>();
            resourceRegistry.Add(new ResourceDictionary { Source = new Uri("Micser.Main;component/Themes/Generic.xaml", UriKind.Relative) });

            var regionManager = containerProvider.Resolve<IRegionManager>();
            regionManager.RequestNavigate("MainRegion", new Uri(nameof(MainView), UriKind.Relative));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IWidgetFactory, WidgetFactory>();
            containerRegistry.RegisterWidget<DeviceInputWidget, DeviceInputViewModel>(Resources.DeviceInputWidgetName);
            containerRegistry.RegisterWidget<DeviceOutputWidget, DeviceOutputViewModel>(Resources.DeviceOutputWidgetName);

            containerRegistry.RegisterView<MainView, MainViewModel>("MainRegion");
        }
    }
}