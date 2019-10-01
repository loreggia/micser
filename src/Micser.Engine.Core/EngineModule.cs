using Microsoft.Extensions.Configuration;
using Micser.Common;
using Micser.Common.Audio;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Micser.Engine.Api;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;

namespace Micser.Engine
{
    public class EngineModule : IEngineModule
    {
        public void OnInitialized(IContainerProvider container)
        {
            var settingsRegistry = container.Resolve<ISettingsRegistry>();

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

        public void RegisterTypes(IContainerProvider container)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json", false);
            var configuration = configurationBuilder.Build();
            container.RegisterInstance<IConfiguration>(configuration);

            container.RegisterRequestProcessor<EngineProcessor>();
            container.RegisterRequestProcessor<StatusProcessor>();
            container.RegisterRequestProcessor<ModulesProcessor>();
            container.RegisterRequestProcessor<ModuleConnectionsProcessor>();
            container.RegisterRequestProcessor<SettingsProcessor>();

            container.RegisterSingleton<IAudioEngine, AudioEngine>();
        }
    }
}