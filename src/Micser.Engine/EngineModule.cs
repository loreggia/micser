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
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddNlog("Micser.Engine.log");

            services.AddGrpc();

            services.AddDbContext<EngineDbContext>(configuration.GetConnectionString("DefaultConnection"));

            services.AddSingleton(typeof(IRpcStreamService<>), typeof(RpcStreamService<>));
            services.AddSingleton<IAudioEngine, AudioEngine>();

            services.AddHostedService<MicserService>();
        }

        public void Initialize(IApplicationBuilder app)
        {
            using var dbContext = app.ApplicationServices.GetRequiredService<IDbContextFactory<EngineDbContext>>().CreateDbContext();
            dbContext.Database.Migrate();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<EngineApiService>();
                endpoints.MapGrpcService<ModuleConnectionsApiService>();
                endpoints.MapGrpcService<ModulesApiService>();
                endpoints.MapGrpcService<SettingsApiService>();
                endpoints.MapGrpcService<EngineEventsApiService>();
            });

            var settingsRegistry = app.ApplicationServices.GetRequiredService<ISettingsRegistry>();

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