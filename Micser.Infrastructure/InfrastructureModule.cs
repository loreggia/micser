using System;
using Micser.Infrastructure.Extensions;
using Micser.Infrastructure.Menu;
using Micser.Infrastructure.ViewModels;
using Micser.Infrastructure.Views;
using Prism.Modularity;
using Prism.Regions;
using Unity;

namespace Micser.Infrastructure
{
    public class InfrastructureModule : IModule
    {
        private readonly IUnityContainer _container;
        private readonly IMenuItemRegistry _menuItemRegistry;
        private readonly IRegionManager _regionManager;

        public InfrastructureModule(IUnityContainer container, IRegionManager regionManager, IMenuItemRegistry menuItemRegistry)
        {
            _container = container;
            _regionManager = regionManager;
            _menuItemRegistry = menuItemRegistry;
        }

        public void Initialize()
        {
            _container.RegisterSingleton<IConfigurationService, ConfigurationService>();

            // top level menu items
            _menuItemRegistry.Add(new MenuItemDescription { Header = "_File", Id = "File" });
            _menuItemRegistry.Add(new MenuItemDescription { Header = "_Tools", Id = "Tools" });
            _menuItemRegistry.Add(new MenuItemDescription { Header = "_Help", Id = "Help" });

            _menuItemRegistry.Add(new MenuItemDescription { Header = "E_xit", Id = "Exit", ParentId = "File", Command = CustomApplicationCommands.Exit });
            _menuItemRegistry.Add(new MenuItemDescription { Header = "_Options", Id = "Options", ParentId = "Tools" });
            _menuItemRegistry.Add(new MenuItemDescription { Header = "_About", Id = "About", ParentId = "Help" });

            _container.RegisterView<MainMenuView, MainMenuViewModel>("MenuRegion");
            _container.RegisterView<MainStatusBarView, MainStatusBarViewModel>("StatusRegion");

            _regionManager.RequestNavigate("MenuRegion", new Uri(nameof(MainMenuView), UriKind.Relative));
            _regionManager.RequestNavigate("StatusRegion", new Uri(nameof(MainStatusBarView), UriKind.Relative));
        }
    }
}