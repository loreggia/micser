using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common.Audio;
using Micser.Common.Extensions;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;

namespace Micser.Engine
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseModules();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddNlog("Micser.Engine.log");
            services.AddModules<IEngineModule>(typeof(EngineModule), typeof(InfrastructureModule));

            services.AddGrpc();

            services.AddSingleton<IAudioEngine, AudioEngine>();
            services.AddHostedService<MicserService>();
        }
    }
}