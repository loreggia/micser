using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Audio;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Micser.Engine.Api;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.DataAccess;

namespace Micser.Engine
{
    public class EngineModule : IEngineModule
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<EngineApiService>();
                endpoints.MapGrpcService<ModuleConnectionsApiService>();
                endpoints.MapGrpcService<ModulesApiService>();
                endpoints.MapGrpcService<SettingsApiService>();
                endpoints.MapGrpcService<EngineEventsApiService>();
            });
        }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddNlog("Micser.Engine.log");

            services.AddGrpc();

            services.AddDbContext<EngineDbContext>(configuration.GetConnectionString("DefaultConnection"));

            services.AddSingleton(typeof(IRpcStreamService<>), typeof(RpcStreamService<>));
            services.AddSingleton<IAudioEngine, AudioEngine>();

            services.AddHostedService<MicserService>();
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            using var dbContext = serviceProvider.GetRequiredService<IDbContextFactory<EngineDbContext>>().CreateDbContext();
            dbContext.Database.Migrate();

            var settingsRegistry = serviceProvider.GetRequiredService<ISettingsRegistry>();

            settingsRegistry.Add(new SettingDefinition
            {
                Key = Globals.SettingKeys.UpdateCheck,
                DefaultValue = true
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = Globals.SettingKeys.IsEngineRunning,
                DefaultValue = true
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = Globals.SettingKeys.ResumeDelay,
                DefaultValue = 10
            });
        }
    }
}