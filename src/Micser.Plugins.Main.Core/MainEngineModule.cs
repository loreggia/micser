using Micser.Common;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.Extensions;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main
{
    public class MainEngineModule : IEngineModule
    {
        public void OnInitialized(IContainerProvider container)
        {
        }

        public void RegisterTypes(IContainerProvider container)
        {
            container.RegisterAudioModules(
                typeof(DeviceInputModule),
                typeof(LoopbackDeviceInputModule),
                typeof(DeviceOutputModule),
                typeof(CompressorModule),
                typeof(GainModule),
                typeof(SpectrumModule),
                typeof(PitchModule));
        }
    }
}