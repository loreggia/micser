using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common;
using Micser.Common.Settings;
using Micser.Engine.Api;
using Micser.Engine.Infrastructure;

namespace Micser.Engine
{
    public class EngineModule : IEngineModule
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
        }

        public void Initialize(IApplicationBuilder app)
        {
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