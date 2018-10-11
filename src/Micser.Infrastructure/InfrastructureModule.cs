using Micser.Infrastructure.Extensions;
using Micser.Infrastructure.Menu;
using Micser.Infrastructure.ViewModels;
using Micser.Infrastructure.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace Micser.Infrastructure
{
    public class InfrastructureModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // top level menu items
            var menuItemRegistry = containerProvider.Resolve<IMenuItemRegistry>();
            menuItemRegistry.Add(new MenuItemDescription { Header = "_File", Id = "File" });
            menuItemRegistry.Add(new MenuItemDescription { Header = "_Tools", Id = "Tools" });
            menuItemRegistry.Add(new MenuItemDescription { Header = "_Help", Id = "Help" });

            menuItemRegistry.Add(new MenuItemDescription { Header = "E_xit", Id = "Exit", ParentId = "File", Command = CustomApplicationCommands.Exit });
            menuItemRegistry.Add(new MenuItemDescription { Header = "_Options", Id = "Options", ParentId = "Tools" });
            menuItemRegistry.Add(new MenuItemDescription { Header = "_About", Id = "About", ParentId = "Help" });

            var navigationManager = containerProvider.Resolve<INavigationManager>();
            navigationManager.Navigate<MainStatusBarView>(Globals.PrismRegions.Status);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterView<MainMenuView, MainMenuViewModel>(Globals.PrismRegions.Menu);

            containerRegistry.RegisterView<MainStatusBarView, MainStatusBarViewModel>(Globals.PrismRegions.Status);

            containerRegistry.RegisterView<StartupView, StartupViewModel>(Globals.PrismRegions.Main);
            containerRegistry.RegisterView<MainView, MainViewModel>(Globals.PrismRegions.Main);
        }
    }
}