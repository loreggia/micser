using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common;
using Micser.Common.Extensions;
using Micser.Common.UI;
using Micser.Plugins.Main.Modules;
using Micser.Plugins.Main.Pages;

namespace Micser.Plugins.Main
{
    public class MainEngineModule : IModule
    {
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

            services.AddScoped<IWidget, TestWidget>();
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
        }
    }
}