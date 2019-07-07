using Micser.Common.Extensions;
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