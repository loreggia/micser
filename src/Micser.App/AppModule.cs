using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
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
                Name = Resources.SettingExitOnCloseName,
                Description = Resources.SettingExitOnCloseDescription,
                DefaultValue = true,
                Type = SettingType.Boolean
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.Startup,
                Name = Resources.SettingStartupName,
                Description = Resources.SettingStartupDescription,
                DefaultValue = true,
                Type = SettingType.Boolean,
                StorageType = SettingStorageType.Custom,
                Handler = containerProvider.Resolve<StartupSettingHandler>()
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.ColorTheme,
                Name = Resources.SettingColorThemeName,
                Description = Resources.SettingColorThemeDescription,
                DefaultValue = "Default",
                Type = SettingType.List,
                StorageType = SettingStorageType.Internal,
                Handler = containerProvider.Resolve<ColorThemeSettingHandler>()
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.ShellState,
                Type = SettingType.Object,
                StorageType = SettingStorageType.Internal,
                IsHidden = true
            });

            var navigationManager = containerProvider.Resolve<INavigationManager>();

            var menuItemRegistry = containerProvider.Resolve<IMenuItemRegistry>();

            // File
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemFileHeader, Id = AppGlobals.MenuItemIds.File });
            // File->Load
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemLoadHeader,
                Id = AppGlobals.MenuItemIds.FileLoad,
                ParentId = AppGlobals.MenuItemIds.File,
                Command = CustomApplicationCommands.Load,
                IconTemplateName = "Icon_OpenFile_16x"
            });
            // File->Save
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemSaveHeader,
                Id = AppGlobals.MenuItemIds.FileSave,
                ParentId = AppGlobals.MenuItemIds.File,
                Command = CustomApplicationCommands.Save,
                IconTemplateName = "Icon_Save_16x"
            });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.File });
            // File->Exit
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemExitHeader,
                Id = AppGlobals.MenuItemIds.FileExit,
                ParentId = AppGlobals.MenuItemIds.File,
                Command = CustomApplicationCommands.Exit
            });

            // Tools
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemToolsHeader, Id = AppGlobals.MenuItemIds.Tools });
            // Tools->Refresh
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemRefreshHeader,
                Id = AppGlobals.MenuItemIds.ToolsRefresh,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = CustomApplicationCommands.Refresh,
                IconTemplateName = "Icon_Refresh_16x"
            });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.Tools });
            // Tools->Start
            var engineApiClient = containerProvider.Resolve<EngineApiClient>();
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemStartHeader,
                Id = AppGlobals.MenuItemIds.ToolsStart,
                ParentId = AppGlobals.MenuItemIds.Tools,
                // todo canexecute
                // , () => !engineApiClient.GetStatusAsync().ConfigureAwait(true).GetAwaiter().GetResult().Data
                Command = new DelegateCommand(async () => await engineApiClient.StartAsync()),
                IconTemplateName = "Icon_Start_16x"
            });
            // Tools->Stop
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemStopHeader,
                Id = AppGlobals.MenuItemIds.ToolsStop,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new DelegateCommand(async () => await engineApiClient.StopAsync()),
                IconTemplateName = "Icon_Stop_16x"
            });
            // Tools->Restart
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemRestartHeader,
                Id = AppGlobals.MenuItemIds.ToolsRestart,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new DelegateCommand(async () => await engineApiClient.RestartAsync()),
                IconTemplateName = "Icon_Restart_16x"
            });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.Tools });
            // Tools->Settings
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemSettingsHeader,
                Id = AppGlobals.MenuItemIds.ToolsSettings,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new NavigationCommand<SettingsView>(AppGlobals.PrismRegions.Main),
                IconTemplateName = "Icon_Settings_16x"
            });

            // Help
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemHelpHeader, Id = AppGlobals.MenuItemIds.Help });
            // Help->About
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemAboutHeader,
                Id = AppGlobals.MenuItemIds.HelpAbout,
                ParentId = AppGlobals.MenuItemIds.Help,
                Command = new NavigationCommand<AboutView>(AppGlobals.PrismRegions.Main),
                IconTemplateName = "Icon_HelpApplication_16x"
            });

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
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = CustomApplicationCommands.Delete,
                Description = Resources.ToolBarDeleteDescription,
                IconTemplateName = "Icon_Delete_16x"
            });
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