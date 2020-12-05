using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Micser.Audio;
using Micser.Common;
using Micser.Common.Audio;
using Micser.Common.Events;
using Micser.Common.Extensions;
using Micser.Common.Services;
using Micser.Common.Settings;
using Micser.Common.Updates;
using Micser.DataAccess;
using Micser.Services;
using Micser.Settings;

namespace Micser
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

            app.ApplicationServices.UseModules();

            var dbContextFactory = app.ApplicationServices.GetRequiredService<IDbContextFactory<EngineDbContext>>();
            using var dbContext = dbContextFactory.CreateDbContext();
            dbContext.Database.Migrate();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddNlog("Micser.Engine.log");

            services.AddModules<IPlugin>(Configuration, typeof(EnginePlugin));

            services.AddDbContext<EngineDbContext>(Configuration.GetConnectionString("DefaultConnection"));

            services.AddSingleton<IEventAggregator, EventAggregator>();
            services.AddSingleton<IAudioEngine, AudioEngine>();

            services.AddTransient<IModuleService, ModuleService>();
            services.AddTransient<IModuleConnectionService, ModuleConnectionService>();

            services.AddSingleton<ISettingsRegistry, SettingsRegistry>();
            services.AddSingleton<ISettingsService, SettingsService<EngineDbContext>>();
            services.AddSingleton<ISettingHandlerFactory>(sp => new SettingHandlerFactory(t => (ISettingHandler)sp.GetService(t)));

            services.AddSingleton<IUpdateService, HttpUpdateService>();

            services.AddHostedService<MicserService>();
        }

        private void OnStopping()
        {
            Console.WriteLine(new StackTrace(true));
        }
    }
}