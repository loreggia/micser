using Micser.Infrastructure.Menu;
using Micser.Infrastructure.Themes;
using Prism.Ioc;
using Prism.Modularity;

namespace Micser.Core
{
    public class CoreModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IResourceRegistry, ResourceRegistry>();
            containerRegistry.RegisterSingleton<IMenuItemRegistry, MenuItemRegistry>();
        }
    }
}