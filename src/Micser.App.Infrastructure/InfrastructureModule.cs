using Micser.App.Infrastructure.Menu;
using Micser.App.Infrastructure.Themes;
using Micser.App.Infrastructure.ToolBars;
using Micser.App.Infrastructure.Widgets;
using Prism.Ioc;

namespace Micser.App.Infrastructure
{
    public class InfrastructureModule : IAppModule
    {
        private static bool _isRegistered;

        public static void RegisterInfrastructureTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<INavigationManager, NavigationManager>();

            containerRegistry.RegisterSingleton<IApplicationStateService, ApplicationStateService>();

            containerRegistry.RegisterSingleton<IResourceRegistry, ResourceRegistry>();
            containerRegistry.RegisterSingleton<IMenuItemRegistry, MenuItemRegistry>();
            containerRegistry.RegisterSingleton<IToolBarRegistry, ToolBarRegistry>();
            containerRegistry.RegisterSingleton<IWidgetRegistry, WidgetRegistry>();
            containerRegistry.RegisterSingleton<IWidgetFactory, WidgetFactory>();

            _isRegistered = true;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            if (_isRegistered)
            {
                return;
            }

            RegisterInfrastructureTypes(containerRegistry);
        }
    }
}