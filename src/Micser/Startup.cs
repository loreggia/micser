using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Micser.Audio;
using Micser.Common;
using Micser.Common.Audio;
using Micser.Common.Events;
using Micser.Common.Extensions;
using Micser.Common.Services;
using Micser.Common.Settings;
using Micser.Common.Updates;
using Micser.DataAccess;
using Micser.Extensions;
using Micser.Infrastructure;
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

        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment, ILogger<Startup> logger)
        {
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(OnStopping);

            app.ApplicationServices.UsePlugins();

            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseApiExceptionHandler(logger);

            app.UseRouting();

            app.UseStaticFiles();
            app.UseSpecificSpaStaticFiles(options =>
                options
                    .WithRootPath(environment.IsDevelopment()
                        ? "UI/public"
                        : "UI/build")
                    .WithRequestPaths("/locales")
            );
            app.UseEndpoints(endpoints => endpoints.MapControllers());

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "UI";
                if (environment.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer("start");
                }
            });

            var dbContextFactory = app.ApplicationServices.GetRequiredService<IDbContextFactory<EngineDbContext>>();
            using var dbContext = dbContextFactory.CreateDbContext();
            dbContext.Database.Migrate();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddNlog("Micser.Engine.log");

            services.AddControllers()
                .AddNewtonsoftJson();

            services.AddSignalR(options =>
            {
                // increase max message size for spectrum data
                options.MaximumReceiveMessageSize = 1024 * 1000;
            });

            services.AddSingleton<IFileProvider>(sp => new PhysicalFileProvider(sp.GetRequiredService<IWebHostEnvironment>().ContentRootPath, ExclusionFilters.Sensitive));

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration => configuration.RootPath = "UI/build");

            services.AddPlugins<IPlugin>(Configuration, typeof(EnginePlugin));

            services.AddDbContext<EngineDbContext>(Configuration.GetConnectionString("DefaultConnection"));

            services.AddSingleton<IEventAggregator, EventAggregator>();

            services.AddSingleton<DeviceService>();
            services.AddSingleton<IAudioEngine, AudioEngine>();

            services.AddTransient<IModuleService, ModuleService>();
            services.AddTransient<IModuleConnectionService, ModuleConnectionService>();

            services.AddSingleton<ISettingsService, SettingsService<EngineDbContext>>();
            services.AddSingleton<ISettingHandlerFactory>(sp => new SettingHandlerFactory(t => (ISettingHandler?)sp.GetService(t)));

            services.AddSingleton<IUpdateService, HttpUpdateService>();

            services.AddHostedService<MicserService>();
        }

        private void OnStopping()
        {
            Console.WriteLine(new StackTrace(true));
        }
    }
}