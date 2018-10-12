using Micser.Infrastructure.Extensions;
using Micser.Infrastructure.Menu;
using Micser.Infrastructure.Themes;
using Micser.Infrastructure.ViewModels;
using Micser.Infrastructure.Views;
using Micser.Infrastructure.Widgets;
using Prism.Ioc;
using Prism.Modularity;

namespace Micser.Infrastructure
{
    public class InfrastructureModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<IApplicationStateService>().Initialize();

            // top level menu items
            var menuItemRegistry = containerProvider.Resolve<IMenuItemRegistry>();
            menuItemRegistry.Add(new MenuItemDescription { Header = "_File", Id = "File" });
            menuItemRegistry.Add(new MenuItemDescription { Header = "_Tools", Id = "Tools" });
            menuItemRegistry.Add(new MenuItemDescription { Header = "_Help", Id = "Help" });

            menuItemRegistry.Add(new MenuItemDescription { Header = "E_xit", Id = "Exit", ParentId = "File", Command = CustomApplicationCommands.Exit });
            menuItemRegistry.Add(new MenuItemDescription { Header = "_Options", Id = "Options", ParentId = "Tools" });
            menuItemRegistry.Add(new MenuItemDescription { Header = "_About", Id = "About", ParentId = "Help" });

            var navigationManager = containerProvider.Resolve<INavigationManager>();
            navigationManager.Navigate<StartupView>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IApplicationStateService, ApplicationStateService>();
            containerRegistry.RegisterSingleton<IResourceRegistry, ResourceRegistry>();
            containerRegistry.RegisterSingleton<IMenuItemRegistry, MenuItemRegistry>();
            containerRegistry.RegisterSingleton<IWidgetRegistry, WidgetRegistry>();
            containerRegistry.RegisterSingleton<IWidgetFactory, WidgetFactory>();
            containerRegistry.RegisterSingleton<INavigationManager, NavigationManager>();

            containerRegistry.RegisterView<StartupView, StartupViewModel>();

            containerRegistry.RegisterView<MainMenuView, MainMenuViewModel>();
            containerRegistry.RegisterView<MainStatusBarView, MainStatusBarViewModel>();
            containerRegistry.RegisterView<MainView, MainViewModel>();
        }
    }
}