using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.Updates;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Timers;
using Unity;

namespace Micser.Engine
{
    public partial class MicserService : ServiceBase
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly ICollection<IEngineModule> _plugins;
        private readonly Timer _reconnectTimer;
        private readonly Timer _updateTimer;
        private IAudioEngine _engine;
        private IApiServer _server;
        private ISettingsService _settingsService;
        private IUpdateService _updateService;

        static MicserService()
        {
            Directory.CreateDirectory(Globals.AppDataFolder);
        }

        public MicserService()
        {
            InitializeComponent();

            _plugins = new List<IEngineModule>();
            _reconnectTimer = new Timer(1000) { AutoReset = false };
            _reconnectTimer.Elapsed += OnReconnectTimerElapsed;
            _updateTimer = new Timer(1000) { AutoReset = false };
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
        }

        public void ManualStart()
        {
            Logger.Info("Starting service");

            var container = new UnityContainer();

            LoadPlugins(container);

            _server = container.Resolve<IApiServer>();
            _engine = container.Resolve<IAudioEngine>();
            _updateService = container.Resolve<IUpdateService>();

            _server.Start();
            _engine.Start();

            _settingsService = container.Resolve<ISettingsService>();
            _settingsService.LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            _reconnectTimer.Start();
            _updateTimer.Start();

            Logger.Info("Service started");
        }

        public void ManualStop()
        {
            Logger.Info("Stopping service");

            _reconnectTimer.Stop();
            _updateTimer.Stop();

            _server.Stop();
            _engine.Stop();

            _plugins.Clear();

            Logger.Info("Service stopped");
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                ManualStart();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while starting the service.");
            }
        }

        protected override void OnStop()
        {
            try
            {
                ManualStop();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while stopping the service.");
            }
        }

        private void LoadPlugins(IUnityContainer container)
        {
            Logger.Info("Loading plugins");

            _plugins.Clear();

            _plugins.Add(new EngineModule());
            _plugins.Add(new InfrastructureModule());

            var executingFile = new FileInfo(Assembly.GetExecutingAssembly().Location);

            foreach (var moduleFile in executingFile.Directory.GetFiles(Globals.PluginSearchPattern))
            {
                try
                {
                    var assembly = Assembly.LoadFile(moduleFile.FullName);
                    var moduleTypes = assembly.GetExportedTypes().Where(t => typeof(IEngineModule).IsAssignableFrom(t));
                    container.RegisterSingletons<IEngineModule>(moduleTypes);
                    var modules = container.ResolveAll<IEngineModule>();
                    foreach (var engineModule in modules)
                    {
                        _plugins.Add(engineModule);
                        Logger.Info($"Loading plugin {engineModule.GetType().AssemblyQualifiedName}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex);
                }
            }

            foreach (var engineModule in _plugins)
            {
                engineModule.RegisterTypes(container);
            }

            foreach (var engineModule in _plugins)
            {
                engineModule.OnInitialized(container);
            }

            Logger.Info("Plugins loaded");
        }

        private async void OnReconnectTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_server.State == EndPointState.Disconnected)
            {
                if (!await _server.ConnectAsync())
                {
                    _reconnectTimer.Start();
                }
            }
        }

        private async void OnUpdateTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var interval = TimeSpan.FromHours(24).TotalMilliseconds;
            _updateTimer.Interval = interval;

            var updatesEnabled = _settingsService.GetSetting<bool>(Globals.SettingKeys.UpdateCheck);

            if (updatesEnabled)
            {
                var updateManifest = await _updateService.GetUpdateManifestAsync();

                if (updateManifest != null)
                {
                    var updateVersion = new Version(updateManifest.Version);
                    var currentVersion = Assembly.GetEntryAssembly().GetName().Version;

                    if (updateVersion > currentVersion)
                    {
                        await _server.SendMessageAsync(new JsonRequest(Globals.ApiResources.Updates, "updateavailable", updateManifest));
                    }
                }
            }

            _updateTimer.Start();
        }
    }
}