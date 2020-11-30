using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Micser.Common.Audio;
using Micser.Common.Extensions;
using Micser.Engine;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.DataAccess;
using Micser.Engine.Infrastructure.Extensions;

[assembly: HostingStartup(typeof(EngineHostingStartup))]

namespace Micser.Engine
{
    public class EngineHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    services.AddNlog("Micser.Engine.log");

                    services.AddModules<IEngineModule>(configuration, typeof(EngineModule), typeof(InfrastructureModule));

                    services.AddDbContext<EngineDbContext>(configuration.GetConnectionString("DefaultConnection"));

                    services.AddSingleton<IAudioEngine, AudioEngine>();

                    services.AddHostedService<MicserService>();
                })
                .Configure((context, app) =>
                {
                    var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
                    lifetime.ApplicationStopping.Register(OnStopping);

                    app.UseModules();

                    using var dbContext = app.ApplicationServices.GetRequiredService<EngineDbContext>();
                    dbContext.Database.Migrate();
                });
        }

        private void OnStopping()
        {
            Console.WriteLine(new StackTrace(true));
        }
    }
}