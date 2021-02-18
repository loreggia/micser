using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common;
using Micser.Common.Extensions;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main
{
    public class MainPlugin : IPlugin
    {
        public string UIModuleName => "micser-plugins-main";

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddAudioModule<DeviceInputModule>("DeviceInput");
            services.AddAudioModule<LoopbackDeviceInputModule>("LoopbackDeviceInput");
            services.AddAudioModule<DeviceOutputModule>("DeviceOutput");
            services.AddAudioModule<CompressorModule>("Compressor");
            services.AddAudioModule<GainModule>("Gain");
            services.AddAudioModule<SpectrumModule>("Spectrum");
            services.AddAudioModule<PitchModule>("Pitch");
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
        }
    }
}