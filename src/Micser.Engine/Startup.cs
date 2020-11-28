using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime)
        {
            lifetime.ApplicationStopping.Register(OnStopping);

            app.UseModules();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddModules<IEngineModule>(Configuration, typeof(EngineModule), typeof(InfrastructureModule));
        }

        private void OnStopping()
        {
            Console.WriteLine(new StackTrace(true));
        }
    }
}