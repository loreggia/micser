using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common;
using Micser.Common.Settings;

namespace Micser.Engine
{
    public class EngineModule : IModule
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
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