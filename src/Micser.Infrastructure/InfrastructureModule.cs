using System;
using Micser.Infrastructure.Extensions;
using Micser.Infrastructure.Menu;
using Micser.Infrastructure.ViewModels;
using Micser.Infrastructure.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;

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

            var regionManager = containerProvider.Resolve<IRegionManager>();
            regionManager.RequestNavigate("MenuRegion", new Uri(nameof(MainMenuView), UriKind.Relative));
            regionManager.RequestNavigate("StatusRegion", new Uri(nameof(MainStatusBarView), UriKind.Relative));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterView<MainMenuView, MainMenuViewModel>("MenuRegion");
            containerRegistry.RegisterView<MainStatusBarView, MainStatusBarViewModel>("StatusRegion");
        }
    }
}