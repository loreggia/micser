using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.Commands;
using Micser.App.Infrastructure.DataAccess;
using Micser.App.Infrastructure.Extensions;
using Micser.App.Infrastructure.Interaction;
using Micser.App.Infrastructure.Localization;
using Micser.App.Infrastructure.Menu;
using Micser.App.Infrastructure.Navigation;
using Micser.App.Infrastructure.Themes;
using Micser.App.Infrastructure.ToolBars;
using Micser.App.Infrastructure.Updates;
using Micser.App.Infrastructure.Widgets;
using Micser.App.Resources;
using Micser.App.Settings;
using Micser.App.ViewModels;
using Micser.App.Views;
using Micser.Common;
using Micser.Common.Api;
using Micser.Common.DataAccess;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Micser.Common.Updates;
using NLog;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using File = System.IO.File;

namespace Micser.App
{
    public class AppModule : IAppModule
    {
        public AppModule()
        {
            LocalizationManager.UiCultureChanged += OnUiCultureChanged;
        }

        public void OnInitialized(IContainerProvider container)
        {
            using (var dbContext = container.Resolve<IDbContextFactory>().Create())
            {
                dbContext.Database.Migrate();
            }

            RegisterSettings(container);
            RegisterMenuItems(container);
            RegisterToolBarItems(container);

            InitializeShell(container);

            container.Resolve<IApplicationStateService>().Initialize();
        }

        public void RegisterTypes(IContainerProvider container)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json", false);
            var configuration = configurationBuilder.Build();
            container.RegisterInstance<IConfiguration>(configuration);

            container.RegisterFactory<ILogger>(c => LogManager.GetCurrentClassLogger());

            container.RegisterInstance<IDbContextFactory>(new DbContextFactory(() => new AppDbContext(container.GetDbContextOptions<AppDbContext>())));

            container.RegisterSingleton<INavigationManager, NavigationManager>();

            container.RegisterSingleton<IApplicationStateService, ApplicationStateService>();

            container.RegisterSingleton<IResourceRegistry, ResourceRegistry>();
            container.RegisterSingleton<IMenuItemRegistry, MenuItemRegistry>();
            container.RegisterSingleton<IToolBarRegistry, ToolBarRegistry>();
            container.RegisterSingleton<IWidgetRegistry, WidgetRegistry>();
            container.RegisterSingleton<IWidgetFactory, WidgetFactory>();

            container.RegisterSingleton<IRequestProcessorFactory, RequestProcessorFactory>();
            container.RegisterType<IRequestProcessor, ApiEventRequestProcessor>();
            container.RegisterSingleton<IApiServer, ApiServer>();
            container.RegisterInstance<IApiServerConfiguration>(new ApiConfiguration { PipeName = Globals.AppPipeName });
            container.RegisterSingleton<IApiClient, ApiClient>();
            container.RegisterInstance<IApiClientConfiguration>(new ApiConfiguration { PipeName = Globals.EnginePipeName });
            container.RegisterSingleton<IMessageSerializer, MessagePackMessageSerializer>();

            container.RegisterSingleton<ISettingsRegistry, SettingsRegistry>();
            container.RegisterSingleton<ISettingsService, SettingsService>();
            container.RegisterInstance<ISettingHandlerFactory>(new SettingHandlerFactory(t => (ISettingHandler)container.Resolve(t)));

            container.RegisterDialog<MessageBoxView, MessageBoxViewModel>();

            container.RegisterView<MainMenuView, MainMenuViewModel>();
            container.RegisterView<MainStatusBarView, MainStatusBarViewModel>();
            container.RegisterView<ToolBarView, ToolBarViewModel>();

            container.RegisterView<StartupView, StartupViewModel>();
            container.RegisterView<StatusView, StatusViewModel>();
            container.RegisterView<MainView, MainViewModel>();
            container.RegisterView<SettingsView, SettingsViewModel>();
            container.RegisterView<AboutView, AboutViewModel>();

            container.RegisterRequestProcessor<UpdatesRequestProcessor>();
            container.RegisterSingleton<IUpdateService, HttpUpdateService>();
            container.RegisterSingleton<UpdateHandler>();
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

        private static bool IsVacInstalled()
        {
            return File.Exists(@"Driver\Micser.Vac.Driver.sys");
        }

        private static object Localize(string key)
        {
            return new ResourceElement(Strings.ResourceManager, key);
        }

        private static void OnUiCultureChanged(object sender, EventArgs e)
        {
            Strings.Culture = LocalizationManager.UiCulture;
        }

        private static void RegisterMenuItems(IContainerProvider containerProvider)
        {
            var menuItemRegistry = containerProvider.Resolve<IMenuItemRegistry>();
            var navigationManager = containerProvider.Resolve<INavigationManager>();

            // File
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemFileHeader)),
                Id = AppGlobals.MenuItemIds.File
            });
            // File->Import
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemImportHeader)),
                Id = AppGlobals.MenuItemIds.FileImport,
                ParentId = AppGlobals.MenuItemIds.File,
                Command = CustomApplicationCommands.Import,
                IconTemplateName = "Icon_OpenFile_16x"
            });
            // File->Export
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemExportHeader)),
                Id = AppGlobals.MenuItemIds.FileExport,
                ParentId = AppGlobals.MenuItemIds.File,
                Command = CustomApplicationCommands.Export,
                IconTemplateName = "Icon_Save_16x"
            });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.File });
            // File->Exit
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemExitHeader)),
                Id = AppGlobals.MenuItemIds.FileExit,
                ParentId = AppGlobals.MenuItemIds.File,
                Command = CustomApplicationCommands.Exit
            });

            // Tools
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemToolsHeader)),
                Id = AppGlobals.MenuItemIds.Tools
            });
            // Tools->Refresh
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemRefreshHeader)),
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
                Header = Localize(nameof(Strings.MenuItemStartHeader)),
                Id = AppGlobals.MenuItemIds.ToolsStart,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new AsyncDelegateCommand(async () => await engineApiClient.StartAsync(), async () => !(await engineApiClient.GetStatusAsync()).IsSuccess),
                IconTemplateName = "Icon_Start_16x"
            });
            // Tools->Stop
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemStopHeader)),
                Id = AppGlobals.MenuItemIds.ToolsStop,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new AsyncDelegateCommand(async () => await engineApiClient.StopAsync(), async () => (await engineApiClient.GetStatusAsync()).IsSuccess),
                IconTemplateName = "Icon_Stop_16x"
            });
            // Tools->Restart
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemRestartHeader)),
                Id = AppGlobals.MenuItemIds.ToolsRestart,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new AsyncDelegateCommand(async () => await engineApiClient.RestartAsync(), async () => (await engineApiClient.GetStatusAsync()).IsSuccess),
                IconTemplateName = "Icon_Restart_16x"
            });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.Tools });
            // Tools->Settings
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemSettingsHeader)),
                Id = AppGlobals.MenuItemIds.ToolsSettings,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new NavigationCommand<SettingsView>(navigationManager, AppGlobals.PrismRegions.Main),
                IconTemplateName = "Icon_Settings_16x"
            });

            // Help
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemHelpHeader)),
                Id = AppGlobals.MenuItemIds.Help
            });
            var updateHandler = containerProvider.Resolve<UpdateHandler>();
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemCheckUpdateHeader)),
                Id = AppGlobals.MenuItemIds.HelpCheckUpdate,
                ParentId = AppGlobals.MenuItemIds.Help,
                Command = new DelegateCommand(async () => await updateHandler.CheckForUpdateAsync()),
                IconTemplateName = "Icon_WebAPI_16x"
            });
            menuItemRegistry.Add(new MenuItemDescription { IsSeparator = true, ParentId = AppGlobals.MenuItemIds.Help });
            // Help->About
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemAboutHeader)),
                Id = AppGlobals.MenuItemIds.HelpAbout,
                ParentId = AppGlobals.MenuItemIds.Help,
                Command = new NavigationCommand<AboutView>(navigationManager, AppGlobals.PrismRegions.Main),
                IconTemplateName = "Icon_HelpApplication_16x"
            });
        }

        private static void RegisterSettings(IContainerProvider containerProvider)
        {
            var settingsRegistry = containerProvider.Resolve<ISettingsRegistry>();

            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.Language,
                Name = Localize(nameof(Strings.SettingLanguageName)),
                Description = Localize(nameof(Strings.SettingLanguageDescription)),
                DefaultValue = "en",
                Type = SettingType.List,
                StorageType = SettingStorageType.Internal,
                HandlerType = typeof(LanguageSettingHandler)
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.MinimizeToTray,
                Name = Localize(nameof(Strings.SettingMinimizeToTrayName)),
                Description = Localize(nameof(Strings.SettingMinimizeToTrayDescription)),
                DefaultValue = true,
                Type = SettingType.Boolean
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.Startup,
                Name = Localize(nameof(Strings.SettingStartupName)),
                Description = Localize(nameof(Strings.SettingStartupDescription)),
                DefaultValue = true,
                Type = SettingType.Boolean,
                StorageType = SettingStorageType.Custom,
                HandlerType = typeof(StartupSettingHandler)
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.StartupMinimized,
                Name = Localize(nameof(Strings.SettingStartupMinimizedName)),
                Description = Localize(nameof(Strings.SettingStartupMinimizedDescription)),
                DefaultValue = true,
                Type = SettingType.Boolean,
                StorageType = SettingStorageType.Internal,
                IsEnabled = s => s.GetSetting<bool>(AppGlobals.SettingKeys.Startup)
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = AppGlobals.SettingKeys.ColorTheme,
                Name = Localize(nameof(Strings.SettingColorThemeName)),
                Description = Localize(nameof(Strings.SettingColorThemeDescription)),
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

            if (IsVacInstalled())
            {
                settingsRegistry.Add(new SettingDefinition
                {
                    Key = AppGlobals.SettingKeys.VacCount,
                    Name = Localize(nameof(Strings.SettingVacCountName)),
                    Description = Localize(nameof(Strings.SettingVacCountDescription)),
                    Type = SettingType.List,
                    StorageType = SettingStorageType.Custom,
                    DefaultValue = 1,
                    List = Enumerable.Range(1, Globals.MaxVacCount).ToDictionary(x => (object)x, x => x.ToString(CultureInfo.InvariantCulture)),
                    HandlerType = typeof(VacCountSettingHandler),
                    IsAppliedInstantly = false
                });
            }

            settingsRegistry.Add(new SettingDefinition
            {
                Key = Globals.SettingKeys.UpdateCheck,
                Name = Localize(nameof(Strings.SettingUpdateCheckName)),
                Description = Localize(nameof(Strings.SettingUpdateCheckDescription)),
                Type = SettingType.Boolean,
                StorageType = SettingStorageType.Api,
                DefaultValue = true
            });
            settingsRegistry.Add(new SettingDefinition
            {
                Key = Globals.SettingKeys.ResumeDelay,
                Name = Localize(nameof(Strings.SettingResumeDelayName)),
                Description = Localize(nameof(Strings.SettingResumeDelayDescription)),
                Type = SettingType.Integer,
                StorageType = SettingStorageType.Api,
                DefaultValue = 10
            });

            settingsRegistry.Add(new SettingDefinition
            {
                IsHidden = true,
                Key = AppGlobals.SettingKeys.IsWidgetToolBoxOpen,
                DefaultValue = true
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
                Description = Localize(nameof(Strings.ToolBarHomeDescription)),
                IconTemplateName = "Icon_Home_16x"
            });

            var goBackCommand = new DelegateCommand(() => navigationManager.GoBack(AppGlobals.PrismRegions.Main), () => navigationManager.CanGoBack(AppGlobals.PrismRegions.Main));
            eventAggregator.GetEvent<ApplicationEvents.Navigated>().Subscribe(info => goBackCommand.RaiseCanExecuteChanged());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = goBackCommand,
                Description = Localize(nameof(Strings.ToolBarBackDescription)),
                IconTemplateName = "Icon_Backward_16x"
            });

            var goForwardCommand = new DelegateCommand(() => navigationManager.GoForward(AppGlobals.PrismRegions.Main), () => navigationManager.CanGoForward(AppGlobals.PrismRegions.Main));
            eventAggregator.GetEvent<ApplicationEvents.Navigated>().Subscribe(info => goForwardCommand.RaiseCanExecuteChanged());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = goForwardCommand,
                Description = Localize(nameof(Strings.ToolBarForwardDescription)),
                IconTemplateName = "Icon_Forward_16x"
            });

            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarSeparator());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = CustomApplicationCommands.Delete,
                Description = Localize(nameof(Strings.ToolBarDeleteDescription)),
                IconTemplateName = "Icon_Delete_16x"
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = CustomApplicationCommands.Refresh,
                Description = Localize(nameof(Strings.ToolBarRefreshDescription)),
                IconTemplateName = "Icon_Refresh_16x"
            });
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarSeparator());
            toolBarRegistry.AddItem(AppGlobals.ToolBarIds.Main, new ToolBarButton
            {
                Command = new NavigationCommand<SettingsView>(navigationManager, AppGlobals.PrismRegions.Main),
                Description = Localize(nameof(Strings.ToolBarSettingsDescription)),
                IconTemplateName = "Icon_Settings_16x"
            });
        }
    }
}