using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Menu;
using Micser.App.Infrastructure.Themes;
using Micser.App.Properties;
using Micser.App.ViewModels;
using Micser.App.Views;
using Prism.Ioc;

namespace Micser.App
{
    public class AppModule : IAppModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // top level menu items
            var menuItemRegistry = containerProvider.Resolve<IMenuItemRegistry>();
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemFileHeader, Id = "File" });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemToolsHeader, Id = "Tools" });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemHelpHeader, Id = "Help" });

            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemExitHeader, Id = "Exit", ParentId = "File", Command = CustomApplicationCommands.Exit });

            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemSettingsHeader, Id = "Settings", ParentId = "Tools", Command = new NavigationCommand<SettingsView>() });
            //menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemAboutHeader, Id = "About", ParentId = "Help", Command = new NavigationCommand<AboutView>() });

            var navigationManager = containerProvider.Resolve<INavigationManager>();
            navigationManager.Navigate<StartupView>();

            containerProvider.Resolve<IApplicationStateService>().Initialize();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IApplicationStateService, ApplicationStateService>();
            containerRegistry.RegisterSingleton<IResourceRegistry, ResourceRegistry>();
            containerRegistry.RegisterSingleton<IMenuItemRegistry, MenuItemRegistry>();
            containerRegistry.RegisterSingleton<INavigationManager, NavigationManager>();

            containerRegistry.RegisterView<StartupView, StartupViewModel>();
            containerRegistry.RegisterView<StatusView, StatusViewModel>();

            containerRegistry.RegisterView<MainMenuView, MainMenuViewModel>();
            containerRegistry.RegisterView<MainStatusBarView, MainStatusBarViewModel>();
            containerRegistry.RegisterView<MainView, MainViewModel>();
        }
    }
}