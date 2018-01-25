using Micser.Infrastructure.Menu;
using Micser.Infrastructure.Themes;
using Prism.Modularity;
using Unity;

namespace Micser.Core
{
    public class CoreModule : IModule
    {
        private readonly IUnityContainer _container;

        public CoreModule(IUnityContainer container)
        {
            _container = container;
        }

        public void Initialize()
        {
            _container.RegisterSingleton<IResourceRegistry, ResourceRegistry>();
            _container.RegisterSingleton<IMenuItemRegistry, MenuItemRegistry>();
        }
    }
}