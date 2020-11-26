using System;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Localization;
using Micser.App.Infrastructure.Themes;
using Micser.Plugins.Main.Resources;
using Micser.Plugins.Main.Widgets;

namespace Micser.Plugins.Main
{
    public class MainAppModule : IAppModule
    {
        public MainAppModule()
        {
            LocalizationManager.UiCultureChanged += OnUiCultureChanged;
        }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            container.RegisterWidget<DeviceInputWidget, DeviceInputViewModel>(
                Localize(nameof(Strings.DeviceInputWidgetName)),
                Localize(nameof(Strings.DeviceInputWidgetDescription)));
            container.RegisterWidget<DeviceInputWidget, LoopbackDeviceInputViewModel>(
                Localize(nameof(Strings.LoopbackDeviceInputWidgetName)),
                Localize(nameof(Strings.LoopbackDeviceInputWidgetDescription)));
            container.RegisterWidget<DeviceOutputWidget, DeviceOutputViewModel>(
                Localize(nameof(Strings.DeviceOutputWidgetName)),
                Localize(nameof(Strings.DeviceOutputWidgetDescription)));
            container.RegisterWidget<CompressorWidget, CompressorViewModel>(
                Localize(nameof(Strings.CompressorWidgetName)),
                Localize(nameof(Strings.CompressorWidgetDescription)));
            container.RegisterWidget<GainWidget, GainViewModel>(
                Localize(nameof(Strings.GainWidgetName)),
                Localize(nameof(Strings.GainWidgetDescription)));
            container.RegisterWidget<SpectrumWidget, SpectrumViewModel>(
                Localize(nameof(Strings.SpectrumWidgetName)),
                Localize(nameof(Strings.SpectrumWidgetDescription)));
            container.RegisterWidget<PitchWidget, PitchViewModel>(
                Localize(nameof(Strings.PitchWidgetName)),
                Localize(nameof(Strings.PitchWidgetDescription)));
        }

        public void Initialize(IApplicationBuilder app)
        {
            var resourceRegistry = app.ApplicationServices.GetRequiredService<IResourceRegistry>();
            resourceRegistry.Add(new ResourceDictionary { Source = new Uri("Micser.Plugins.Main;component/Themes/Generic.xaml", UriKind.Relative) });
        }

        private static object Localize(string key)
        {
            return new ResourceElement(Strings.ResourceManager, key);
        }

        private static void OnUiCultureChanged(object sender, EventArgs e)
        {
            Strings.Culture = LocalizationManager.UiCulture;
        }
    }
}