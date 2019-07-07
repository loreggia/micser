using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Menu;
using Micser.App.Infrastructure.ToolBars;
using Micser.App.Properties;
using Micser.App.Settings;
using Micser.App.ViewModels;
using Micser.App.Views;
using Micser.Common;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using System.Globalization;
using System.Linq;

namespace Micser.App
{
    public class AppModule : IAppModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var settingsRegistry = containerProvider.Resolve<ISettingsRegistry>();
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.MinimizeToTray,
                Name = Resources.SettingMinimizeToTrayName,
                Description = Resources.SettingMinimizeToTrayDescription,
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
                HandlerType = typeof(StartupSettingHandler)
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.StartupMinimized,
                Name = Resources.SettingStartupMinimizedName,
                Description = Resources.SettingStartupMinimizedDescription,
                DefaultValue = true,
                Type = SettingType.Boolean,
                StorageType = SettingStorageType.Internal,
                IsEnabled = s => s.GetSetting<bool>(AppGlobals.SettingKeys.Startup)
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.ColorTheme,
                Name = Resources.SettingColorThemeName,
                Description = Resources.SettingColorThemeDescription,
                DefaultValue = "Default",
                Type = SettingType.List,
                StorageType = SettingStorageType.Internal,
                HandlerType = typeof(ColorThemeSettingHandler)
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.ShellState,
                Type = SettingType.Object,
                StorageType = SettingStorageType.Internal,
                IsHidden = true
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.VacCount,
                Name = Resources.SettingsVacCountName,
                Description = Resources.SettingsVacCountDescription,
                Type = SettingType.List,
                StorageType = SettingStorageType.Custom,
                DefaultValue = 1,
                List = Enumerable.Range(1, Globals.MaxVacCount).ToDictionary(x => (object)x, x => x.ToString(CultureInfo.InvariantCulture)),
                HandlerType = typeof(VacCountSettingHandler),
                IsAppliedInstantly = false
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = Globals.SettingKeys.UpdateCheck,
                Name = Resources.SettingsUpdateCheckName,
                Description = Resources.SettingsUpdateCheckDescription,
                Type = SettingType.Boolean,
                StorageType = SettingStorageType.Api,
                DefaultValue = true
            });

            var navigationManager = containerProvider.Resolve<INavigationManager>();
            var menuItemRegistry = containerProvider.Resolve<IMenuItemRegistry>();

            // File
            menuItemRegistry.Add(new MenuItemDescription { Header = Resources.MenuItemFileHeader, Id = AppGlobals.MenuItemIds.File });
            // File->Import
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemImportHeader,
                Id = AppGlobals.MenuItemIds.FileImport,
                ParentId = AppGlobals.MenuItemIds.File,
                Command = CustomApplicationCommands.Import,
                IconTemplateName = "Icon_OpenFile_16x"
            });
            // File->Export
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemExportHeader,
                Id = AppGlobals.MenuItemIds.FileExport,
                ParentId = AppGlobals.MenuItemIds.File,
                Command = CustomApplicationCommands.Export,
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
                Command = new AsyncDelegateCommand(async () => await engineApiClient.StartAsync(), async () => !(await engineApiClient.GetStatusAsync()).IsSuccess),
                IconTemplateName = "Icon_Start_16x"
            });
            // Tools->Stop
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemStopHeader,
                Id = AppGlobals.MenuItemIds.ToolsStop,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new AsyncDelegateCommand(async () => await engineApiClient.StopAsync(), async () => (await engineApiClient.GetStatusAsync()).IsSuccess),
                IconTemplateName = "Icon_Stop_16x"
            });
            // Tools->Restart
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Resources.MenuItemRestartHeader,
                Id = AppGlobals.MenuItemIds.ToolsRestart,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new AsyncDelegateCommand(async () => await engineApiClient.RestartAsync(), async () => (await engineApiClient.GetStatusAsync()).IsSuccess),
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