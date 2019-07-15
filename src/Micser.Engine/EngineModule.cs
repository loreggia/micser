using Micser.Common;
using Micser.Common.Audio;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Micser.Engine.Api;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Unity;

namespace Micser.Engine
{
    public class EngineModule : IEngineModule
    {
        public void OnInitialized(IUnityContainer container)
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
        }

        public void RegisterTypes(IUnityContainer container)
        {
            container.RegisterRequestProcessor<EngineProcessor>();
            container.RegisterRequestProcessor<StatusProcessor>();
            container.RegisterRequestProcessor<ModulesProcessor>();
            container.RegisterRequestProcessor<ModuleConnectionsProcessor>();
            container.RegisterRequestProcessor<SettingsProcessor>();

            container.RegisterSingleton<IAudioEngine, AudioEngine>();
        }
    }
}