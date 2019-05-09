using CommonServiceLocator;
using Micser.App.Infrastructure;
using Micser.App.Infrastructure.DataAccess;
using Micser.App.Infrastructure.Settings;
using Micser.App.Infrastructure.Themes;
using Micser.App.Settings;
using Micser.Common;
using Micser.Common.DataAccess;
using NLog;
using NLog.Config;
using NLog.Targets;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Unity;
using Unity.Injection;
using Unity.Resolution;

namespace Micser.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class Application
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private bool _isStartup;
        private MainShell _shell;

        public Application()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        public static T GetService<T>()
        {
            return ServiceLocator.Current.GetInstance<T>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);

            moduleCatalog.AddModule<AppModule>();
            moduleCatalog.AddModule<InfrastructureModule>();

            LoadPlugins(moduleCatalog);
        }

        protected override Window CreateShell()
        {
            _shell = GetService<MainShell>();
            return _shell;
        }

        protected override void InitializeModules()
        {
            SetStatus("Initializing...");

            base.InitializeModules();

            var resourceRegistry = GetService<IResourceRegistry>();
            foreach (var dictionary in resourceRegistry.Items)
            {
                Current.Resources.MergedDictionaries.Add(dictionary);
            }

            var eventAggregator = GetService<IEventAggregator>();
            var modulesLoadedEvent = eventAggregator.GetEvent<ApplicationEvents.ModulesLoaded>();
            modulesLoadedEvent.Publish();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_shell != null)
            {
                var state = new ShellState
                {
                    Width = _shell.ActualWidth,
                    Height = _shell.ActualHeight,
                    Top = _shell.Top,
                    Left = _shell.Left
                };

                var settingsService = GetService<ISettingsService>();
                settingsService.SetSetting(AppGlobals.SettingKeys.ShellState, state);

                _shell = null;
            }

            base.OnExit(e);

            Logger.Info("Exiting...");
        }

        protected override async void OnInitialized()
        {
            var settingsService = GetService<ISettingsService>();

            await Task.Run(() => settingsService.LoadAsync());

            var showWindow = true;

            if (_isStartup)
            {
                var startMinimized = settingsService.GetSetting<bool>(AppGlobals.SettingKeys.StartupMinimized);

                if (startMinimized)
                {
                    showWindow = false;
                }
            }

            if (showWindow)
            {
                MainWindow?.Show();
            }

            Logger.Info("Ready");
            SetStatus("Ready");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var argumentDictionary = new ArgumentDictionary(e.Args);
            _isStartup = argumentDictionary.HasFlag(AppGlobals.ProgramArguments.Startup);

            Directory.CreateDirectory(Globals.AppDataFolder);

            var config = new LoggingConfiguration();
            config.AddTarget(new ColoredConsoleTarget("ConsoleTarget")
            {
                Layout = @"${date:format=HH\:mm\:ss} ${level} ${message} ${exception}",
                DetectConsoleAvailable = true
            });
            config.AddTarget(new FileTarget("FileTarget")
            {
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                ArchiveOldFileOnStartup = true,
                MaxArchiveFiles = 10,
                FileName = Path.Combine(Globals.AppDataFolder, "Micser.App.log"),
                FileNameKind = FilePathKind.Absolute
            });
            config.AddRuleForAllLevels("ConsoleTarget");
            config.AddRuleForAllLevels("FileTarget");

            LogManager.Configuration = config;
            Logger.Info("Starting...");

            base.OnStartup(e);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // register types that MainShell depends on here..
            var container = containerRegistry.GetContainer();
            container.RegisterType<ILogger>(new InjectionFactory(c => LogManager.GetCurrentClassLogger()));

            container.RegisterType<DbContext, AppDbContext>();
            container.RegisterInstance<IRepositoryFactory>(new RepositoryFactory((t, c) => container.Resolve(t, new ParameterOverride("context", c))));
            container.RegisterInstance<IUnitOfWorkFactory>(new UnitOfWorkFactory(() => container.Resolve<IUnitOfWork>()));
            container.RegisterType<IUnitOfWork, UnitOfWork>();

            // DEBUG
            //container.RegisterType<IRegionNavigationJournal, MicserRegionNavigationJournal>();
        }

        private static void LoadPlugins(IModuleCatalog moduleCatalog)
        {
            Logger.Debug("Loading plugins");
            SetStatus("Loading plugins...");

            var executingFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var moduleFiles = executingFile.Directory.GetFiles(Globals.PluginSearchPattern);
            foreach (var moduleFile in moduleFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFile(moduleFile.FullName);
                    var moduleTypes = assembly.GetExportedTypes().Where(t => typeof(IAppModule).IsAssignableFrom(t));
                    foreach (var moduleType in moduleTypes)
                    {
                        if (moduleCatalog.Modules.All(m => m.ModuleType != moduleType.AssemblyQualifiedName))
                        {
                            moduleCatalog.AddModule(moduleType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                }
            }

            Logger.Debug("Plugins loaded");
        }

        private static void SetStatus(string text)
        {
            var eventAggregator = GetService<IEventAggregator>();
            var statusChangeEvent = eventAggregator.GetEvent<ApplicationEvents.StatusChange>();
            statusChangeEvent.Publish(text);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error(e.Exception, "Unhandled application exception.");
            e.Handled = true;
        }
    }
}