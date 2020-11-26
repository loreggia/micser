using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common;
using Micser.Common.Audio;
using Micser.Common.Settings;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;

namespace Micser.Engine
{
    public class EngineModule : IEngineModule
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            //services.AddRequestProcessor<EngineProcessor>();
            //services.AddRequestProcessor<StatusProcessor>();
            //services.AddRequestProcessor<ModulesProcessor>();
            //services.AddRequestProcessor<ModuleConnectionsProcessor>();
            //services.AddRequestProcessor<SettingsProcessor>();

            services.AddSingleton<IAudioEngine, AudioEngine>();
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