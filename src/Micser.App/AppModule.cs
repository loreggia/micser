using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Menu;
using Micser.App.Infrastructure.Settings;
using Micser.App.Infrastructure.ToolBars;
using Micser.App.Properties;
using Micser.App.Settings;
using Micser.App.ViewModels;
using Micser.App.Views;
using Prism.Commands;
using Prism.Events;
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
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.Startup,
                Name = "Startup",
                Description = "If enabled, the program will start when you log in.",
                DefaultValue = true,
                Type = SettingType.Boolean,
                StorageType = SettingStorageType.Custom,
                GetCustomSetting = StartupSettingHelper.GetStartupSetting,
                SetCustomSetting = StartupSettingHelper.SetStartupSetting
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = "IntTest",
                Name = "Integer test",
                DefaultValue = 42,
                Type = SettingType.Integer
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = "DecimalTest",
                Name = "Decimal test",
                DefaultValue = 42.123456789,
                Type = SettingType.Decimal
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = "StringTest",
                Name = "String test",
                DefaultValue = "Bla bla bla",
                Type = SettingType.String
            });

            var navigationManager = containerProvider.Resolve<INavigationManager>();

            var menuItemRegistry = containerProvider.Resolve<IMenuItemRegistry>();

            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemFileHeader, Id = AppGlobals.MenuItemIds.File });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemLoadHeader, Id = AppGlobals.MenuItemIds.FileLoad, ParentId = AppGlobals.MenuItemIds.File, Command = CustomApplicationCommands.Load, IconTemplateName = "Icon_OpenFile_16x" });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemSaveHeader, Id = AppGlobals.MenuItemIds.FileSave, ParentId = AppGlobals.MenuItemIds.File, Command = CustomApplicationCommands.Save, IconTemplateName = "Icon_Save_16x" });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.File });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemExitHeader, Id = AppGlobals.MenuItemIds.FileExit, ParentId = AppGlobals.MenuItemIds.File, Command = CustomApplicationCommands.Exit });

            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemToolsHeader, Id = AppGlobals.MenuItemIds.Tools });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemRefreshHeader, Id = AppGlobals.MenuItemIds.ToolsRefresh, ParentId = AppGlobals.MenuItemIds.Tools, Command = CustomApplicationCommands.Refresh, IconTemplateName = "Icon_Refresh_16x" });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.Tools });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemSettingsHeader, Id = AppGlobals.MenuItemIds.ToolsSettings, ParentId = AppGlobals.MenuItemIds.Tools, Command = new NavigationCommand<SettingsView>(AppGlobals.PrismRegions.Main), IconTemplateName = "Icon_Settings_16x" });

            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemHelpHeader, Id = AppGlobals.MenuItemIds.Help });
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemAboutHeader, Id = AppGlobals.MenuItemIds.HelpAbout, ParentId = AppGlobals.MenuItemIds.Help, Command = new NavigationCommand<AboutView>(AppGlobals.PrismRegions.Main), IconTemplateName = "Icon_HelpApplication_16x" });

            // main tool bar
            var toolBarRegistry = containerProvider.Resolve<IToolBarRegistry>();
            var eventAggregator = containerProvider.Resolve<IEventAggregator>();

            var goHomeCommand = new DelegateCommand(() => navigationManager.Navigate<MainView>(AppGlobals.PrismRegions.Main));
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = goHomeCommand,
                Description = Resources.ToolBarHomeDescription,
                IconTemplateName = "Icon_Home_16x"
            });

            var goBackCommand = new DelegateCommand(() => navigationManager.GoBack(AppGlobals.PrismRegions.Main), () => navigationManager.CanGoBack(AppGlobals.PrismRegions.Main));
            eventAggregator.GetEvent<ApplicationEvents.Navigated>().Subscribe(info => goBackCommand.RaiseCanExecuteChanged());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = goBackCommand,
                Description = Resources.ToolBarBackDescription,
                IconTemplateName = "Icon_Backward_16x"
            });

            var goForwardCommand = new DelegateCommand(() => navigationManager.GoForward(AppGlobals.PrismRegions.Main), () => navigationManager.CanGoForward(AppGlobals.PrismRegions.Main));
            eventAggregator.GetEvent<ApplicationEvents.Navigated>().Subscribe(info => goForwardCommand.RaiseCanExecuteChanged());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = goForwardCommand,
                Description = Resources.ToolBarForwardDescription,
                IconTemplateName = "Icon_Forward_16x"
            });

            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarSeparator());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = CustomApplicationCommands.Load,
                Description = Resources.ToolBarLoadDescription,
                IconTemplateName = "Icon_OpenFile_16x"
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = CustomApplicationCommands.Save,
                Description = Resources.ToolBarSaveDescription,
                IconTemplateName = "Icon_Save_16x"
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarSeparator());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = CustomApplicationCommands.Refresh,
                Description = Resources.ToolBarRefreshDescription,
                IconTemplateName = "Icon_Refresh_16x"
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarSeparator());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = new NavigationCommand<SettingsView>(AppGlobals.PrismRegions.Main),
                Description = Resources.ToolBarSettingsDescription,
                IconTemplateName = "Icon_Settings_16x"
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