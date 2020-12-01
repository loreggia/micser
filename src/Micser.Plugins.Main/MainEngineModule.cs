using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.Extensions;
using Micser.Plugins.Main.Modules;
using Micser.Plugins.Main.Pages;
using Micser.UI.Shared;

namespace Micser.Plugins.Main
{
    public class MainEngineModule : IEngineModule
    {
        public void Configure(IApplicationBuilder app)
        {
            //app.UseEndpoints(ep => ep.MapGrpcService<SpectrumApiService>());
        }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddAudioModules(
                typeof(DeviceInputModule),
                typeof(LoopbackDeviceInputModule),
                typeof(DeviceOutputModule),
                typeof(CompressorModule),
                typeof(GainModule),
                //typeof(SpectrumModule),
                typeof(PitchModule));

            services.AddScoped<IWidget, TestWidget>();
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
        }
    }
}