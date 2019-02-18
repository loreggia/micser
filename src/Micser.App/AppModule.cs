using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Menu;
using Micser.App.Infrastructure.Settings;
using Micser.App.Infrastructure.ToolBars;
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
            var settingsRegistry = containerProvider.Resolve<ISettingsRegistry>();
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.ExitOnClose,
                Name = "Exit on close",
                Description = "If enabled, the program will minimize to the system tray instead of exiting when the window is closed.",
                DefaultValue = true,
                Type = SettingType.Boolean
            });

            var navigationManager = containerProvider.Resolve<INavigationManager>();

            var menuItemRegistry = containerProvider.Resolve<IMenuItemRegistry>();

            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemFileHeader, Id = AppGlobals.MenuItemIds.File });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemLoadHeader, Id = AppGlobals.MenuItemIds.FileLoad, ParentId = AppGlobals.MenuItemIds.File, IconResourceName = "OpenFile_16x" });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemSaveHeader, Id = AppGlobals.MenuItemIds.FileSave, ParentId = AppGlobals.MenuItemIds.File, IconResourceName = "Save_16x" });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.File });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemExitHeader, Id = AppGlobals.MenuItemIds.FileExit, ParentId = AppGlobals.MenuItemIds.File, Command = CustomApplicationCommands.Exit });

            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemToolsHeader, Id = AppGlobals.MenuItemIds.Tools });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemRefreshHeader, Id = AppGlobals.MenuItemIds.ToolsRefresh, ParentId = AppGlobals.MenuItemIds.Tools, IconResourceName = "Refresh_16x" });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.Tools });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemSettingsHeader, Id = AppGlobals.MenuItemIds.ToolsSettings, ParentId = AppGlobals.MenuItemIds.Tools, Command = new NavigationCommand<SettingsView>(AppGlobals.PrismRegions.Main), IconResourceName = "Settings_16x" });

            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemHelpHeader, Id = AppGlobals.MenuItemIds.Help });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemAboutHeader, Id = AppGlobals.MenuItemIds.HelpAbout, ParentId = AppGlobals.MenuItemIds.Help, Command = new NavigationCommand<AboutView>(AppGlobals.PrismRegions.Main), IconResourceName = "HelpApplication_16x" });

            // main tool bar
            var toolBarRegistry = containerProvider.Resolve<IToolBarRegistry>();
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Action = _ => navigationManager.GoBack(AppGlobals.PrismRegions.Main),
                Description = Resources.ToolBarBackDescription,
                IconResourceName = "Backward_16x"
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarSeparator());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Description = Resources.ToolBarOpenDescription,
                IconResourceName = "OpenFile_16x"
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Description = Resources.ToolBarSaveDescription,
                IconResourceName = "Save_16x"
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarSeparator());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Description = Resources.ToolBarRefreshDescription,
                IconResourceName = "Refresh_16x"
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Name = "Test"
            });

            navigationManager.Navigate<StartupView>(AppGlobals.PrismRegions.Main);

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