using Micser.Infrastructure;
using Micser.Infrastructure.Extensions;
using Micser.Plugins.Main.Modules;
using Unity;

namespace Micser.Plugins.Main
{
    public class MainEngineModule : IEngineModule
    {
        public void RegisterTypes(IUnityContainer container)
        {
            container.RegisterAudioModules(typeof(DeviceInputModule), typeof(DeviceOutputModule));
        }
    }
}