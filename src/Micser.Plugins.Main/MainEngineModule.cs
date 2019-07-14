using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.Extensions;
using Micser.Plugins.Main.Modules;
using Unity;

namespace Micser.Plugins.Main
{
    public class MainEngineModule : IEngineModule
    {
        public void OnInitialized(IUnityContainer container)
        {
        }

        public void RegisterTypes(IUnityContainer container)
        {
            container.RegisterAudioModules(
                typeof(DeviceInputModule),
                typeof(LoopbackDeviceInputModule),
                typeof(DeviceOutputModule),
                typeof(CompressorModule),
                typeof(GainModule),
                typeof(SpectrumModule),
                typeof(RestartEngineModule));
        }
    }
}