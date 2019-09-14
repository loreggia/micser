using CommonServiceLocator;
using Microsoft.Shell;
using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Themes;
using Micser.App.Settings;
using Micser.App.Shortcuts;
using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Settings;
using NLog;
using NLog.Config;
using NLog.Targets;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace Micser.App
{
    public partial class MicserApplication : ISingleInstanceApp
    {
        private const string Unique = "{50CD2933-87A9-4411-9577-56401F034A60}";

        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly Timer _reconnectTimer;
        private IApiEndPoint _apiEndPoint;
        private ArgumentDictionary _argumentDictionary;
        private KeyboardHook _keyboardHook;
        private MainShell _shell;

        public MicserApplication()
        {
            InitializeComponent();

            DispatcherUnhandledException += OnDispatcherUnhandledException;

            _reconnectTimer = new Timer(1000) { AutoReset = false };
            _reconnectTimer.Elapsed += OnReconnectTimerElapsed;
        }

        public static T GetService<T>()
        {
            return ServiceLocator.Current.GetInstance<T>();
        }

        [STAThread]
        public static int Main()
        {
            var enableSingleInstance = !Debugger.IsAttached;

            if (enableSingleInstance && !SingleInstance<MicserApplication>.InitializeAsFirstInstance(Unique))
            {
                return 0;
            }

            var application = new MicserApplication();
            var result = application.Run();

            if (enableSingleInstance)
            {
                // Allow single instance code to perform cleanup operations
                SingleInstance<MicserApplication>.Cleanup();
            }

            return result;
        }

        public override void Initialize()
        {
            base.Initialize();
            InitializeModules();

            _keyboardHook = GetService<KeyboardHook>();
            _keyboardHook.KeyboardPressed += OnKeyboardPressed;
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            if (MainWindow != null)
            {
                MainWindow.Show();

                // Bring window to foreground
                if (MainWindow.WindowState == WindowState.Minimized)
                {
                    MainWindow.WindowState = WindowState.Normal;
                }

                MainWindow.Activate();
            }

            return true;
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
            return null;
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

        protected override void InitializeShell(Window shell)
        {
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _reconnectTimer.Stop();
            _reconnectTimer.Dispose();

            if (_shell != null)
            {
                var state = new ShellState
                {
                    Width = _shell.Width,
                    Height = _shell.Height,
                    Top = _shell.Top,
                    Left = _shell.Left,
                    State = _shell.WindowState
                };

                var settingsService = GetService<ISettingsService>();
                settingsService.SetSettingAsync(AppGlobals.SettingKeys.ShellState, state).GetAwaiter().GetResult();

                _shell = null;
            }

            base.OnExit(e);

            Logger.Info("Exiting...");
        }

        protected override void OnInitialized()
        {
            _apiEndPoint = GetService<IApiEndPoint>();
            _reconnectTimer.Start();

            Logger.Info("Ready");
            SetStatus("Ready");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _argumentDictionary = new ArgumentDictionary(e.Args);

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
            config.AddTarget(new DebuggerTarget("DebuggerTarget"));

            config.AddRuleForAllLevels("ConsoleTarget");
            config.AddRuleForAllLevels("FileTarget");
            config.AddRuleForAllLevels("DebuggerTarget");

            LogManager.Configuration = config;
            Logger.Info("Starting...");

            base.OnStartup(e);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(_argumentDictionary);
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

        private static void OnKeyboardPressed(object sender, KeyboardHookEventArgs e)
        {
            Console.WriteLine($"{e.KeyboardState}, {e.KeyboardData.VirtualCode}");
        }

        private static void SetStatus(string text)
        {
            var eventAggregator = GetService<IEventAggregator>();
            var statusChangeEvent = eventAggregator.GetEvent<ApplicationEvents.StatusChange>();
            statusChangeEvent.Publish(text);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            Logger.Error(e.Exception, "Unhandled application exception.");
            e.Handled = true;
        }

        private async void OnReconnectTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_apiEndPoint != null &&
                _apiEndPoint.State == EndPointState.Disconnected)
            {
                await _apiEndPoint.ConnectAsync().ConfigureAwait(false);
            }

            _reconnectTimer.Start();
        }
    }
}