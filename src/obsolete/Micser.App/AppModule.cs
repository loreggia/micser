using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Commands;
using Micser.App.Infrastructure.DataAccess;
using Micser.App.Infrastructure.Extensions;
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
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Micser.Common.Updates;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using File = System.IO.File;

namespace Micser.App
{
    public class AppModule : IAppModule
    {
        public AppModule()
        {
            LocalizationManager.UiCultureChanged += OnUiCultureChanged;
        }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddNlog("Micser.App.log");

            services.AddDefaultRpcChannel();

            services.Configure<HttpUpdateOptions>(configuration.GetSection(Globals.AppSettingSections.Update.HttpUpdateSettings));

            services.AddDbContext<AppDbContext>(configuration.GetConnectionString("DefaultConnection"));

            services.AddSingleton<INavigationManager, NavigationManager>();

            services.AddSingleton<IApplicationStateService, ApplicationStateService>();

            services.AddSingleton<MainShell>();
            services.AddSingleton<MainShellViewModel>();

            services.AddSingleton<IResourceRegistry, ResourceRegistry>();
            services.AddSingleton<IMenuItemRegistry, MenuItemRegistry>();
            services.AddSingleton<IToolBarRegistry, ToolBarRegistry>();
            services.AddSingleton<IWidgetRegistry, WidgetRegistry>();
            services.AddSingleton<IWidgetFactory, WidgetFactory>();

            services.AddSingleton<ISettingsRegistry, SettingsRegistry>();
            services.AddSingleton<ISettingsService, SettingsService<AppDbContext>>();
            services.AddSingleton<ISettingHandlerFactory>(sp => new SettingHandlerFactory(t => (ISettingHandler)sp.GetRequiredService(t)));

            services.AddTransient<EngineApiClient>();
            services.AddTransient<ModuleConnectionsApiClient>();
            services.AddTransient<ModulesApiClient>();

            // todo
            //services.RegisterDialog<MessageBoxView, MessageBoxViewModel>();
            services.RegisterView<MainMenuView, MainMenuViewModel>();
            services.RegisterView<MainStatusBarView, MainStatusBarViewModel>();
            services.RegisterView<ToolBarView, ToolBarViewModel>();

            services.RegisterView<StartupView, StartupViewModel>();
            services.RegisterView<StatusView, StatusViewModel>();
            services.RegisterView<MainView, MainViewModel>();
            services.RegisterView<SettingsView, SettingsViewModel>();
            services.RegisterView<AboutView, AboutViewModel>();

            services.AddSingleton<IUpdateService, HttpUpdateService>();
            services.AddSingleton<UpdateHandler>();
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            using (var dbContext = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext())
            {
                dbContext.Database.Migrate();
            }

            RegisterSettings(serviceProvider);
            RegisterMenuItems(serviceProvider);
            RegisterToolBarItems(serviceProvider);

            InitializeShell(serviceProvider);

            serviceProvider.GetRequiredService<IApplicationStateService>().Initialize();
        }

        private static async void InitializeShell(IServiceProvider serviceProvider)
        {
            var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
            await settingsService.LoadAsync();

            var shell = serviceProvider.GetRequiredService<MainShell>();
            shell.DataContext = serviceProvider.GetRequiredService<MainShellViewModel>();

            RegionManager.SetRegionManager(shell, serviceProvider.GetRequiredService<IRegionManager>());
            RegionManager.UpdateRegions();

            Application.Current.MainWindow = shell;

            var showWindow = true;

            var argumentDictionary = serviceProvider.GetRequiredService<ArgumentDictionary>();
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

            var navigationManager = serviceProvider.GetRequiredService<INavigationManager>();
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

        private static void RegisterMenuItems(IServiceProvider serviceProvider)
        {
            var menuItemRegistry = serviceProvider.GetRequiredService<IMenuItemRegistry>();
            var navigationManager = serviceProvider.GetRequiredService<INavigationManager>();

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
            var engineApiClient = serviceProvider.GetRequiredService<EngineApiClient>();
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemStartHeader)),
                Id = AppGlobals.MenuItemIds.ToolsStart,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new AsyncDelegateCommand(async () => await engineApiClient.StartAsync(), async () => !(await engineApiClient.GetStatusAsync())),
                IconTemplateName = "Icon_Start_16x"
            });
            // Tools->Stop
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemStopHeader)),
                Id = AppGlobals.MenuItemIds.ToolsStop,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new AsyncDelegateCommand(async () => await engineApiClient.StopAsync(), async () => await engineApiClient.GetStatusAsync()),
                IconTemplateName = "Icon_Stop_16x"
            });
            // Tools->Restart
            menuItemRegistry.Add(new MenuItemDescription
            {
                Header = Localize(nameof(Strings.MenuItemRestartHeader)),
                Id = AppGlobals.MenuItemIds.ToolsRestart,
                ParentId = AppGlobals.MenuItemIds.Tools,
                Command = new AsyncDelegateCommand(async () => await engineApiClient.RestartAsync(), async () => await engineApiClient.GetStatusAsync()),
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
            var updateHandler = serviceProvider.GetRequiredService<UpdateHandler>();
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

        private static void RegisterSettings(IServiceProvider serviceProvider)
        {
            var settingsRegistry = serviceProvider.GetRequiredService<ISettingsRegistry>();

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

        private static void RegisterToolBarItems(IServiceProvider serviceProvider)
        {
            var navigationManager = serviceProvider.GetRequiredService<INavigationManager>();

            // main tool bar
            var toolBarRegistry = serviceProvider.GetRequiredService<IToolBarRegistry>();
            var eventAggregator = serviceProvider.GetRequiredService<IEventAggregator>();

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