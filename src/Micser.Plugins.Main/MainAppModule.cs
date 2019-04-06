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
    public class MainAppModule : IAppModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var resourceRegistry = containerProvider.Resolve<IResourceRegistry>();
            resourceRegistry.Add(new ResourceDictionary { Source = new Uri("Micser.Plugins.Main;component/Themes/Generic.xaml", UriKind.Relative) });
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterWidget<DeviceInputWidget, DeviceInputViewModel>(Resources.DeviceInputWidgetName, Resources.DeviceInputWidgetDescription);
            containerRegistry.RegisterWidget<DeviceInputWidget, LoopbackDeviceInputViewModel>(Resources.LoopbackDeviceInputWidgetName, Resources.LoopbackDeviceInputWidgetDescription);
            containerRegistry.RegisterWidget<DeviceOutputWidget, DeviceOutputViewModel>(Resources.DeviceOutputWidgetName, Resources.DeviceOutputWidgetDescription);
            containerRegistry.RegisterWidget<CompressorWidget, CompressorViewModel>(Resources.CompressorWidgetName, Resources.CompressorWidgetDescription);
            containerRegistry.RegisterWidget<GainWidget, GainViewModel>(Resources.GainWidgetName, Resources.GainWidgetDescription);
        }
    }
}