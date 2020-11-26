using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common.Api;
using Micser.Common.Extensions;
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
            app.UseModules<IEngineModule>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<ApiServer>();
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddNlog("Micser.Engine.log");
            services.AddModules<IEngineModule>();

            services.AddGrpc();

            services.AddHostedService<MicserService>();
        }
    }
}