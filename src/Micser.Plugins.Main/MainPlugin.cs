using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using Micser.Plugins.Main.Modules;
using Micser.Plugins.Main.Resources;

namespace Micser.Plugins.Main
{
    public class MainPlugin : IPlugin
    {
        public string UIModuleName => "micser-plugins-main";

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddAudioModule<DeviceInputModule>(
                new ModuleDescription("DeviceInput", Strings.DeviceInputWidgetName, Strings.DeviceInputWidgetDescription));
            services.AddAudioModule<LoopbackDeviceInputModule>(
                new ModuleDescription("LoopbackDeviceInput", Strings.LoopbackDeviceInputWidgetName, Strings.LoopbackDeviceInputWidgetDescription));
            services.AddAudioModule<DeviceOutputModule>(
                new ModuleDescription("DeviceOutput", Strings.DeviceOutputWidgetName, Strings.DeviceOutputWidgetDescription));
            services.AddAudioModule<CompressorModule>(
                new ModuleDescription("Compressor", Strings.CompressorWidgetName, Strings.CompressorWidgetDescription));
            services.AddAudioModule<GainModule>(
                new ModuleDescription("Gain", Strings.GainWidgetName, Strings.GainWidgetDescription));
            services.AddAudioModule<SpectrumModule>(
                new ModuleDescription("Spectrum", Strings.SpectrumWidgetName, Strings.SpectrumWidgetDescription));
            services.AddAudioModule<PitchModule>(
                new ModuleDescription("Pitch", Strings.PitchWidgetName, Strings.PitchWidgetDescription));
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
        }
    }
}