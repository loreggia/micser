using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Micser.Common.Audio;
using Micser.Common.Extensions;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.DataAccess;
using Micser.Engine.Infrastructure.Extensions;

namespace Micser.Engine
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void Configure(IApplicationBuilder app)
        {
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(OnStopping);

            app.UseModules();

            var dbContextFactory = app.ApplicationServices.GetRequiredService<IDbContextFactory<EngineDbContext>>();
            using var dbContext = dbContextFactory.CreateDbContext();
            dbContext.Database.Migrate();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddNlog("Micser.Engine.log");

            services.AddModules<IEngineModule>(Configuration, typeof(EngineModule), typeof(InfrastructureModule));

            services.AddDbContext<EngineDbContext>(Configuration.GetConnectionString("DefaultConnection"));

            services.AddSingleton<IAudioEngine, AudioEngine>();

            services.AddHostedService<MicserService>();
        }

        private void OnStopping()
        {
            Console.WriteLine(new StackTrace(true));
        }
    }
}