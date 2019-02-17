using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Menu;
using Micser.App.Infrastructure.ToolBars;
using Micser.App.Properties;
using Micser.App.ViewModels;
using Micser.App.Views;
using Prism.Ioc;
using System;

namespace Micser.App
{
    public class AppModule : IAppModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var navigationManager = containerProvider.Resolve<INavigationManager>();

            var menuItemRegistry = containerProvider.Resolve<IMenuItemRegistry>();

            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemFileHeader, Id = AppGlobals.MenuItemIds.File });
            menuItemRegistry.Add(new MenuItemDescription { Header = "Test", Id = "FileTest", ParentId = AppGlobals.MenuItemIds.File });
            menuItemRegistry.Add(new MenuItemDescription { Header = "_Load", Id = "FileLoad", ParentId = "FileTest", IconPath = new Uri("/Micser.App.Infrastructure;component/Images/Icons/Backward_16x.png", UriKind.Relative) });
            menuItemRegistry.Add(new MenuItemDescription { Header = "_Save", Id = "FileSave", ParentId = "FileTest" });
            menuItemRegistry.Add(new MenuItemDescription { Id = "Separator1", IsSeparator = true, ParentId = AppGlobals.MenuItemIds.File });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemExitHeader, Id = AppGlobals.MenuItemIds.FileExit, ParentId = AppGlobals.MenuItemIds.File, Command = CustomApplicationCommands.Exit });

            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemToolsHeader, Id = AppGlobals.MenuItemIds.Tools });
            menuItemRegistry.Add(new MenuItemDescription { Header = "Refresh", Id = "Refresh", ParentId = AppGlobals.MenuItemIds.Tools });
            menuItemRegistry.Add(new MenuItemDescription { Id = "Separator2", IsSeparator = true, ParentId = AppGlobals.MenuItemIds.Tools });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemSettingsHeader, Id = AppGlobals.MenuItemIds.ToolsSettings, ParentId = AppGlobals.MenuItemIds.Tools, Command = new NavigationCommand<SettingsView>(AppGlobals.PrismRegions.Main) });

            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemHelpHeader, Id = AppGlobals.MenuItemIds.Help });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemAboutHeader, Id = AppGlobals.MenuItemIds.HelpAbout, ParentId = AppGlobals.MenuItemIds.Help, Command = new NavigationCommand<AboutView>(AppGlobals.PrismRegions.Main) });

            // main tool bar
            var toolBarRegistry = containerProvider.Resolve<IToolBarRegistry>();
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Action = _ => navigationManager.GoBack(AppGlobals.PrismRegions.Main),
                Description = "Go back",
                Name = "Back",
                IconPath = new Uri("/Micser.App.Infrastructure;component/Images/Icons/Backward_16x.png", UriKind.Relative)
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarSeparator());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Name = "Test 1"
            });

            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarLabel
            {
                Description = "Test desc",
                Name = "Test",
                IconPath = new Uri("/Micser.App.Infrastructure;component/Images/Icons/Backward_16x.png", UriKind.Relative),
                Placement = ToolBarItemPlacement.Overflow
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarSeparator { Placement = ToolBarItemPlacement.Overflow });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton { Name = "Test 2", Placement = ToolBarItemPlacement.Overflow });

            navigationManager.Navigate<StartupView>(AppGlobals.PrismRegions.Main, null, false);

            containerProvider.Resolve<IApplicationStateService>().Initialize();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            InfrastructureModule.RegisterInfrastructureTypes(containerRegistry);

            containerRegistry.RegisterView<MainMenuView, MainMenuViewModel>();
            containerRegistry.RegisterView<MainStatusBarView, MainStatusBarViewModel>();
            containerRegistry.RegisterView<ToolBarView, ToolBarViewModel>();

            containerRegistry.RegisterView<StartupView, StartupViewModel>();
            containerRegistry.RegisterView<StatusView, StatusViewModel>();
            containerRegistry.RegisterView<MainView, MainViewModel>();
            containerRegistry.RegisterView<SettingsView, SettingsViewModel>();
            containerRegistry.RegisterView<AboutView, AboutViewModel>();
        }
    }
}