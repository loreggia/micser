using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.DataAccess;
using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Localization;
using Micser.App.Infrastructure.Menu;
using Micser.App.Infrastructure.Themes;
using Micser.App.Infrastructure.ToolBars;
using Micser.App.Infrastructure.Updates;
using Micser.App.Infrastructure.Widgets;
using Micser.App.Properties;
using Micser.App.Settings;
using Micser.App.ViewModels;
using Micser.App.Views;
using Micser.Common;
using Micser.Common.Api;
using Micser.Common.DataAccess;
using Micser.Common.DataAccess.Repositories;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Micser.Common.Updates;
using NLog;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Prism.Unity;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Windows;
using Unity;
using Unity.Injection;
using Unity.Resolution;

namespace Micser.App
{
    public class AppModule : IAppModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            RegisterSettings(containerProvider);
            RegisterMenuItems(containerProvider);
            RegisterToolBarItems(containerProvider);

            InitializeShell(containerProvider);

            containerProvider.Resolve<IApplicationStateService>().Initialize();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            var container = containerRegistry.GetContainer();
            container.RegisterType<ILogger>(new InjectionFactory(c => LogManager.GetCurrentClassLogger()));

            container.RegisterType<DbContext, AppDbContext>();
            container.RegisterInstance<IRepositoryFactory>(new RepositoryFactory((t, c) => (IRepository)container.Resolve(t, new ParameterOverride("context", c))));
            container.RegisterInstance<IUnitOfWorkFactory>(new UnitOfWorkFactory(() => container.Resolve<IUnitOfWork>()));
            container.RegisterType<IUnitOfWork, UnitOfWork>();

            containerRegistry.RegisterSingleton<INavigationManager, NavigationManager>();

            containerRegistry.RegisterSingleton<IApplicationStateService, ApplicationStateService>();

            containerRegistry.RegisterSingleton<IResourceRegistry, ResourceRegistry>();
            containerRegistry.RegisterSingleton<IMenuItemRegistry, MenuItemRegistry>();
            containerRegistry.RegisterSingleton<IToolBarRegistry, ToolBarRegistry>();
            containerRegistry.RegisterSingleton<IWidgetRegistry, WidgetRegistry>();
            containerRegistry.RegisterSingleton<IWidgetFactory, WidgetFactory>();

            containerRegistry.RegisterSingleton<IRequestProcessorFactory, RequestProcessorFactory>();
            containerRegistry.RegisterSingleton<IApiEndPoint, ApiClient>();
            containerRegistry.RegisterInstance<IApiConfiguration>(new ApiConfiguration { Port = Globals.ApiPort });
            containerRegistry.Register<IRequestProcessor, ApiEventRequestProcessor>();

            containerRegistry.RegisterSingleton<ISettingsRegistry, SettingsRegistry>();
            containerRegistry.RegisterSingleton<ISettingsService, SettingsService>();
            containerRegistry.Register<ISettingValueRepository, SettingValueRepository>();
            container.RegisterInstance<ISettingHandlerFactory>(new SettingHandlerFactory(t => (ISettingHandler)container.Resolve(t)));

            containerRegistry.RegisterView<MainMenuView, MainMenuViewModel>();
            containerRegistry.RegisterView<MainStatusBarView, MainStatusBarViewModel>();
            containerRegistry.RegisterView<ToolBarView, ToolBarViewModel>();

            containerRegistry.RegisterView<StartupView, StartupViewModel>();
            containerRegistry.RegisterView<StatusView, StatusViewModel>();
            containerRegistry.RegisterView<MainView, MainViewModel>();
            containerRegistry.RegisterView<SettingsView, SettingsViewModel>();
            containerRegistry.RegisterView<AboutView, AboutViewModel>();

            container.RegisterRequestProcessor<UpdatesRequestProcessor>();
            containerRegistry.RegisterInstance(new HttpUpdateSettings { ManifestUrl = Globals.Updates.ManifestUrl });
            containerRegistry.RegisterSingleton<IUpdateService, HttpUpdateService>();
            containerRegistry.RegisterSingleton<UpdateHandler>();
        }

        private static async void InitializeShell(IContainerProvider containerProvider)
        {
            var settingsService = containerProvider.Resolve<ISettingsService>();
            await settingsService.LoadAsync();

            var shell = containerProvider.Resolve<MainShell>();
            shell.DataContext = containerProvider.Resolve<MainShellViewModel>();

            RegionManager.SetRegionManager(shell, containerProvider.Resolve<IRegionManager>());
            RegionManager.UpdateRegions();

            Application.Current.MainWindow = shell;

            var showWindow = true;

            var argumentDictionary = containerProvider.Resolve<ArgumentDictionary>();
            var isStartup = argumentDictionary.HasFlag(AppGlobals.ProgramArguments.Startup);

            if (isStartup)
            {
                var startMinimized = settingsService.GetSetting<bool>(AppGlobals.SettingKeys.StartupMinimized);

                if (startMinimized)
                {
                    showWindow = false;
                }
            }

            if (showWindow)
            {
                shell.Show();
            }

            var navigationManager = containerProvider.Resolve<INavigationManager>();
            navigationManager.Navigate<StartupView>(AppGlobals.PrismRegions.Main);
        }

        private static object Localize(string key)
        {
            return new ResourceElement(Resources.ResourceManager, key);
        }

        private static void RegisterMenuItems(IContainerProvider containerProvider)
        {
            var menuItemRegistry = containerProvider.Resolve<IMenuItemRegistry>();

            // File
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Resources.MenuItemFileHeader)),
                Id = AppGlobals.MenuItemIds.File
            });
            // File->Import
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Resources.MenuItemImportHeader)),
                Id = AppGlobals.MenuItemIds.FileImport,
                ParentId = AppGlobals.MenuItemIds.File,
                Command = CustomApplicationCommands.Import,
                IconTemplateName = "Icon_OpenFile_16x"
            });
            // File->Export
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Resources.MenuItemExportHeader)),
                Id = AppGlobals.MenuItemIds.FileExport,
                ParentId = AppGlobals.MenuItemIds.File,
                Command = CustomApplicationCommands.Export,
                IconTemplateName = "Icon_Save_16x"
            });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.File });
            // File->Exit
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Resources.MenuItemExitHeader)),
                Id = AppGlobals.MenuItemIds.FileExit,
                ParentId = AppGlobals.MenuItemIds.File,
                Command = CustomApplicationCommands.Exit
            });

            // Tools
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Resources.MenuItemToolsHeader)),
                Id = AppGlobals.MenuItemIds.Tools
            });
            // Tools->Refresh
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Resources.MenuItemRefreshHeader)),
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
                Header = Localize(nameof(Resources.MenuItemStartHeader)),
                Id = AppGlobals.MenuItemIds.ToolsStart,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new AsyncDelegateCommand(async () => await engineApiClient.StartAsync(), async () => !(await engineApiClient.GetStatusAsync()).IsSuccess),
                IconTemplateName = "Icon_Start_16x"
            });
            // Tools->Stop
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Resources.MenuItemStopHeader)),
                Id = AppGlobals.MenuItemIds.ToolsStop,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new AsyncDelegateCommand(async () => await engineApiClient.StopAsync(), async () => (await engineApiClient.GetStatusAsync()).IsSuccess),
                IconTemplateName = "Icon_Stop_16x"
            });
            // Tools->Restart
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Resources.MenuItemRestartHeader)),
                Id = AppGlobals.MenuItemIds.ToolsRestart,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new AsyncDelegateCommand(async () => await engineApiClient.RestartAsync(), async () => (await engineApiClient.GetStatusAsync()).IsSuccess),
                IconTemplateName = "Icon_Restart_16x"
            });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.Tools });
            // Tools->Settings
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Resources.MenuItemSettingsHeader)),
                Id = AppGlobals.MenuItemIds.ToolsSettings,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new NavigationCommand<SettingsView>(AppGlobals.PrismRegions.Main),
                IconTemplateName = "Icon_Settings_16x"
            });

            // Help
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Resources.MenuItemHelpHeader)),
                Id = AppGlobals.MenuItemIds.Help
            });
            var updateHandler = containerProvider.Resolve<UpdateHandler>();
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Resources.MenuItemCheckUpdateHeader)),
                Id = AppGlobals.MenuItemIds.HelpCheckUpdate,
                ParentId = AppGlobals.MenuItemIds.Help,
                Command = new DelegateCommand(async () => await updateHandler.CheckForUpdateAsync()),
                IconTemplateName = "Icon_WebAPI_16x"
            });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.Help });
            // Help->About
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Resources.MenuItemAboutHeader)),
                Id = AppGlobals.MenuItemIds.HelpAbout,
                ParentId = AppGlobals.MenuItemIds.Help,
                Command = new NavigationCommand<AboutView>(AppGlobals.PrismRegions.Main),
                IconTemplateName = "Icon_HelpApplication_16x"
            });
        }

        private static void RegisterSettings(IContainerProvider containerProvider)
        {
            var settingsRegistry = containerProvider.Resolve<ISettingsRegistry>();

            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.Language,
                Name = Localize(nameof(Resources.SettingLanguageName)),
                Description = Localize(nameof(Resources.SettingLanguageDescription)),
                DefaultValue = "en",
                Type = SettingType.List,
                StorageType = SettingStorageType.Internal,
                HandlerType = typeof(LanguageSettingHandler)
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.MinimizeToTray,
                Name = Localize(nameof(Resources.SettingMinimizeToTrayName)),
                Description = Localize(nameof(Resources.SettingMinimizeToTrayDescription)),
                DefaultValue = true,
                Type = SettingType.Boolean
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.Startup,
                Name = Localize(nameof(Resources.SettingStartupName)),
                Description = Localize(nameof(Resources.SettingStartupDescription)),
                DefaultValue = true,
                Type = SettingType.Boolean,
                StorageType = SettingStorageType.Custom,
                HandlerType = typeof(StartupSettingHandler)
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.StartupMinimized,
                Name = Localize(nameof(Resources.SettingStartupMinimizedName)),
                Description = Localize(nameof(Resources.SettingStartupMinimizedDescription)),
                DefaultValue = true,
                Type = SettingType.Boolean,
                StorageType = SettingStorageType.Internal,
                IsEnabled = s => s.GetSetting<bool>(AppGlobals.SettingKeys.Startup)
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.ColorTheme,
                Name = Localize(nameof(Resources.SettingColorThemeName)),
                Description = Localize(nameof(Resources.SettingColorThemeDescription)),
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
                Name = Localize(nameof(Resources.SettingVacCountName)),
                Description = Localize(nameof(Resources.SettingVacCountDescription)),
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
                Name = Localize(nameof(Resources.SettingUpdateCheckName)),
                Description = Localize(nameof(Resources.SettingUpdateCheckDescription)),
                Type = SettingType.Boolean,
                StorageType = SettingStorageType.Api,
                DefaultValue = true
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = Globals.SettingKeys.ResumeDelay,
                Name = Localize(nameof(Resources.SettingResumeDelayName)),
                Description = Localize(nameof(Resources.SettingResumeDelayDescription)),
                Type = SettingType.Integer,
                StorageType = SettingStorageType.Api,
                DefaultValue = 10
            });
        }

        private static void RegisterToolBarItems(IContainerProvider containerProvider)
        {
            var navigationManager = containerProvider.Resolve<INavigationManager>();

            // main tool bar
            var toolBarRegistry = containerProvider.Resolve<IToolBarRegistry>();
            var eventAggregator = containerProvider.Resolve<IEventAggregator>();

            var goHomeCommand = new DelegateCommand(() => navigationManager.Navigate<MainView>(AppGlobals.PrismRegions.Main));
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = goHomeCommand,
                Description = Localize(nameof(Resources.ToolBarHomeDescription)),
                IconTemplateName = "Icon_Home_16x"
            });

            var goBackCommand = new DelegateCommand(() => navigationManager.GoBack(AppGlobals.PrismRegions.Main), () => navigationManager.CanGoBack(AppGlobals.PrismRegions.Main));
            eventAggregator.GetEvent<ApplicationEvents.Navigated>().Subscribe(info => goBackCommand.RaiseCanExecuteChanged());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = goBackCommand,
                Description = Localize(nameof(Resources.ToolBarBackDescription)),
                IconTemplateName = "Icon_Backward_16x"
            });

            var goForwardCommand = new DelegateCommand(() => navigationManager.GoForward(AppGlobals.PrismRegions.Main), () => navigationManager.CanGoForward(AppGlobals.PrismRegions.Main));
            eventAggregator.GetEvent<ApplicationEvents.Navigated>().Subscribe(info => goForwardCommand.RaiseCanExecuteChanged());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = goForwardCommand,
                Description = Localize(nameof(Resources.ToolBarForwardDescription)),
                IconTemplateName = "Icon_Forward_16x"
            });

            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarSeparator());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = CustomApplicationCommands.Delete,
                Description = Localize(nameof(Resources.ToolBarDeleteDescription)),
                IconTemplateName = "Icon_Delete_16x"
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = CustomApplicationCommands.Refresh,
                Description = Localize(nameof(Resources.ToolBarRefreshDescription)),
                IconTemplateName = "Icon_Refresh_16x"
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarSeparator());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = new NavigationCommand<SettingsView>(AppGlobals.PrismRegions.Main),
                Description = Localize(nameof(Resources.ToolBarSettingsDescription)),
                IconTemplateName = "Icon_Settings_16x"
            });
        }
    }
}