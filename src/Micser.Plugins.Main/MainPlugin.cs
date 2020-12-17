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
        public bool HasUI => true;

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddAudioModules(
                typeof(DeviceInputModule),
                typeof(LoopbackDeviceInputModule),
                typeof(DeviceOutputModule),
                typeof(CompressorModule),
                typeof(GainModule),
                typeof(SpectrumModule),
                typeof(PitchModule));
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
        }
    }
}