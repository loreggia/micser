using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.Extensions;
using Micser.Plugins.Main.Api;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main
{
    public class MainEngineModule : IEngineModule
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
        }

        public void Initialize(IApplicationBuilder app)
        {
            app.UseEndpoints(ep => ep.MapGrpcService<SpectrumApiService>());
        }
    }
}