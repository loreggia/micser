using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Shell;
using Micser.Common;
using Prism.Modularity;

namespace Micser.App
{
    public partial class MicserApplication : Application, ISingleInstanceApp
    {
        private const string Unique = "{50CD2933-87A9-4411-9577-56401F034A60}";

        private ArgumentDictionary _argumentDictionary;
        private MainShell _shell;

        public MicserApplication()
        {
            InitializeComponent();

            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        [STAThread]
        public static int Main(string[] args)
        {
            var enableSingleInstance = !Debugger.IsAttached;

            if (enableSingleInstance && !SingleInstance<MicserApplication>.InitializeAsFirstInstance(Unique, args))
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

        public void SignalExternalCommandLineArgs(string[] args)
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
        }

        protected Window CreateShell()
        {
            return null;
        }

        protected void InitializeModules()
        {
            SetStatus("Initializing...");

            //base.InitializeModules();

            //var resourceRegistry = _containerProvider.Resolve<IResourceRegistry>();
            //foreach (var dictionary in resourceRegistry.Items)
            //{
            //    Current.Resources.MergedDictionaries.Add(dictionary);
            //}

            //var eventAggregator = _containerProvider.Resolve<IEventAggregator>();
            //var modulesLoadedEvent = eventAggregator.GetEvent<ApplicationEvents.ModulesLoaded>();
            //modulesLoadedEvent.Publish();
        }

        protected void InitializeShell(Window shell)
        {
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //var apiServer = _containerProvider.Resolve<IApiServer>();
            //apiServer.Stop();

            //if (_shell != null)
            //{
            //    var state = new ShellState
            //    {
            //        Width = _shell.Width,
            //        Height = _shell.Height,
            //        Top = _shell.Top,
            //        Left = _shell.Left,
            //        State = _shell.WindowState
            //    };

            //    var settingsService = _containerProvider.Resolve<ISettingsService>();
            //    settingsService.SetSettingAsync(AppGlobals.SettingKeys.ShellState, state).GetAwaiter().GetResult();

            //    _shell = null;
            //}

            //base.OnExit(e);

            //Logger.Info("Exiting...");
        }

        protected void OnInitialized()
        {
            //var apiServer = _containerProvider.Resolve<IApiServer>();
            //apiServer.StartAsync();

            //Logger.Info("Ready");
            //SetStatus("Ready");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _argumentDictionary = new ArgumentDictionary(e.Args);
            Directory.CreateDirectory(Globals.AppDataFolder);

            //Logger.Info("Starting...");

            base.OnStartup(e);
        }

        private void LoadPlugins(IModuleCatalog moduleCatalog)
        {
            //Logger.Debug("Loading plugins");
            //SetStatus("Loading plugins...");

            //var executingFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
            //var moduleFiles = executingFile.Directory.GetFiles(Globals.PluginSearchPattern);
            //foreach (var moduleFile in moduleFiles)
            //{
            //    try
            //    {
            //        var assembly = Assembly.LoadFile(moduleFile.FullName);
            //        var moduleTypes = assembly.GetExportedTypes().Where(t => typeof(IAppModule).IsAssignableFrom(t));

            //        foreach (var moduleType in moduleTypes)
            //        {
            //            if (moduleCatalog.Modules.Any(m => m.ModuleType == moduleType.AssemblyQualifiedName))
            //            {
            //                continue;
            //            }

            //            if (typeof(IModule).IsAssignableFrom(moduleType))
            //            {
            //                moduleCatalog.AddModule(moduleType);
            //            }
            //            else
            //            {
            //                var proxyType = typeof(ProxyModule<>).MakeGenericType(moduleType);
            //                moduleCatalog.AddModule(proxyType);
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger.Warn(ex);
            //    }
            //}

            //Logger.Debug("Plugins loaded");
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            //Logger.Error(e.Exception, "Unhandled application exception.");
            e.Handled = true;
        }

        private void SetStatus(string text)
        {
            //var eventAggregator = _containerProvider.Resolve<IEventAggregator>();
            //var statusChangeEvent = eventAggregator.GetEvent<ApplicationEvents.StatusChange>();
            //statusChangeEvent.Publish(text);
        }
    }
}