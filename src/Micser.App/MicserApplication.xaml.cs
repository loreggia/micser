using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Shell;
using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Themes;
using Micser.App.Settings;
using Micser.Common;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Prism;
using Prism.Events;
using Prism.Ioc;
using Prism.Microsoft.DependencyInjection;

namespace Micser.App
{
    public partial class MicserApplication : PrismApplicationBase, ISingleInstanceApp
    {
        private const string Unique = "{50CD2933-87A9-4411-9577-56401F034A60}";

        private ArgumentDictionary _argumentDictionary;
        private IHost _host;
        private CancellationTokenSource _hostCancellation;
        private Task _hostTask;
        private IServiceCollection _services;
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

        protected override IContainerExtension CreateContainerExtension()
        {
            return new PrismContainerExtension(_services);
        }

        protected override Window CreateShell()
        {
            return null;
        }

        protected override void InitializeModules()
        {
            SetStatus("Initializing...");

            base.InitializeModules();

            var modules = Container.Resolve<IEnumerable<IModule>>();

            foreach (var module in modules)
            {
                module.Initialize(Container.Resolve<IServiceProvider>());
            }

            var resourceRegistry = Container.Resolve<IResourceRegistry>();
            foreach (var dictionary in resourceRegistry.Items)
            {
                Current.Resources.MergedDictionaries.Add(dictionary);
            }

            var eventAggregator = Container.Resolve<IEventAggregator>();
            var modulesLoadedEvent = eventAggregator.GetEvent<ApplicationEvents.ModulesLoaded>();
            modulesLoadedEvent.Publish();
        }

        protected override void InitializeShell(Window shell)
        {
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //var apiServer = _containerProvider.Resolve<IApiServer>();
            //apiServer.Stop();

            _hostCancellation.Cancel();

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

                var settingsService = Container.Resolve<ISettingsService>();
                settingsService.SetSettingAsync(AppGlobals.SettingKeys.ShellState, state).GetAwaiter().GetResult();

                _shell = null;
            }

            base.OnExit(e);

            //Logger.Info("Exiting...");
        }

        protected override void OnInitialized()
        {
            // todo
            //var apiServer = _containerProvider.Resolve<IApiServer>();
            //apiServer.StartAsync();

            //Logger.Info("Ready");
            SetStatus("Ready");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            //Logger.Info("Starting...");

            _argumentDictionary = new ArgumentDictionary(e.Args);
            Directory.CreateDirectory(Globals.AppDataFolder);

            _host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddModules<IAppModule>(hostContext.Configuration, typeof(AppModule), typeof(InfrastructureModule));

                    _services = services;
                })
                .Build();
            _hostCancellation = new CancellationTokenSource();
            _hostTask = _host.StartAsync(_hostCancellation.Token);

            base.OnStartup(e);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(_argumentDictionary);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            // todo
            //Logger.Error(e.Exception, "Unhandled application exception.");
            e.Handled = true;
        }

        private void SetStatus(string text)
        {
            var eventAggregator = Container.Resolve<IEventAggregator>();
            var statusChangeEvent = eventAggregator.GetEvent<ApplicationEvents.StatusChange>();
            statusChangeEvent.Publish(text);
        }
    }
}