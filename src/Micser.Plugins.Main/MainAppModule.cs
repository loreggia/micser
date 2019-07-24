using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Localization;
using Micser.App.Infrastructure.Themes;
using Micser.Common.Extensions;
using Micser.Plugins.Main.Api;
using Micser.Plugins.Main.Properties;
using Micser.Plugins.Main.Widgets;
using Prism.Ioc;
using Prism.Unity;
using System;
using System.Windows;

namespace Micser.Plugins.Main
{
    public class MainAppModule : IAppModule
    {
        public MainAppModule()
        {
            LocalizationManager.UiCultureChanged += OnUiCultureChanged;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var resourceRegistry = containerProvider.Resolve<IResourceRegistry>();
            resourceRegistry.Add(new ResourceDictionary { Source = new Uri("Micser.Plugins.Main;component/Themes/Generic.xaml", UriKind.Relative) });
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterWidget<DeviceInputWidget, DeviceInputViewModel>(
                Localize(nameof(Resources.DeviceInputWidgetName)),
                Localize(nameof(Resources.DeviceInputWidgetDescription)));
            containerRegistry.RegisterWidget<DeviceInputWidget, LoopbackDeviceInputViewModel>(
                Localize(nameof(Resources.LoopbackDeviceInputWidgetName)),
                Localize(nameof(Resources.LoopbackDeviceInputWidgetDescription)));
            containerRegistry.RegisterWidget<DeviceOutputWidget, DeviceOutputViewModel>(
                Localize(nameof(Resources.DeviceOutputWidgetName)),
                Localize(nameof(Resources.DeviceOutputWidgetDescription)));
            containerRegistry.RegisterWidget<CompressorWidget, CompressorViewModel>(
                Localize(nameof(Resources.CompressorWidgetName)),
                Localize(nameof(Resources.CompressorWidgetDescription)));
            containerRegistry.RegisterWidget<GainWidget, GainViewModel>(
                Localize(nameof(Resources.GainWidgetName)),
                Localize(nameof(Resources.GainWidgetDescription)));
            containerRegistry.RegisterWidget<SpectrumWidget, SpectrumViewModel>(
                Localize(nameof(Resources.SpectrumWidgetName)),
                Localize(nameof(Resources.SpectrumWidgetDescription)));

            var container = containerRegistry.GetContainer();
            container.RegisterRequestProcessor<SpectrumRequestProcessor>();
        }

        private static object Localize(string key)
        {
            return new ResourceElement(Resources.ResourceManager, key);
        }

        private static void OnUiCultureChanged(object sender, EventArgs e)
        {
            Resources.Culture = LocalizationManager.UiCulture;
        }
    }
}