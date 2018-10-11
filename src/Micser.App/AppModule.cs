using Micser.Infrastructure;
using Micser.Infrastructure.Menu;
using Micser.Infrastructure.Themes;
using Micser.Infrastructure.Widgets;
using Prism.Ioc;
using Prism.Modularity;

namespace Micser.Core
{
    public class AppModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IResourceRegistry, ResourceRegistry>();
            containerRegistry.RegisterSingleton<IMenuItemRegistry, MenuItemRegistry>();
            containerRegistry.RegisterSingleton<IWidgetRegistry, WidgetRegistry>();
            containerRegistry.RegisterSingleton<IWidgetFactory, WidgetFactory>();
            containerRegistry.RegisterSingleton<INavigationManager, NavigationManager>();
        }
    }
}