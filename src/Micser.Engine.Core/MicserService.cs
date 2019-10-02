using Microsoft.Extensions.Hosting;
using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Audio;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Micser.Common.Updates;
using Micser.Engine.Infrastructure;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Micser.Engine
{
    public class MicserService : IHostedService
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly ICollection<IEngineModule> _plugins;
        private readonly Timer _reconnectTimer;
        private readonly Timer _updateTimer;
        private UnityContainerProvider _containerProvider;
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
            _plugins = new List<IEngineModule>();
            _reconnectTimer = new Timer(1000) { AutoReset = false };
            _reconnectTimer.Elapsed += OnReconnectTimerElapsed;
            _updateTimer = new Timer(1000) { AutoReset = false };
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.Info("Starting service");

            _containerProvider = new UnityContainerProvider();

            LoadPlugins(_containerProvider);

            _server = _containerProvider.Resolve<IApiServer>();
            _engine = _containerProvider.Resolve<IAudioEngine>();
            _updateService = _containerProvider.Resolve<IUpdateService>();

            _server.Start();

            _settingsService = _containerProvider.Resolve<ISettingsService>();
            await _settingsService.LoadAsync().ConfigureAwait(false);

            if (_settingsService.GetSetting<bool>(Globals.SettingKeys.IsEngineRunning))
            {
                _engine.Start();
            }

            _reconnectTimer.Start();
            _updateTimer.Start();

            Logger.Info("Service started");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.Info("Stopping service");

            _reconnectTimer.Stop();
            _updateTimer.Stop();

            _server.Stop();
            _engine.Stop();

            _plugins.Clear();

            _containerProvider.Dispose();

            Logger.Info("Service stopped");

            return Task.CompletedTask;
        }

        /*protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            var isSuspending = powerStatus == PowerBroadcastStatus.Suspend;
            var isResuming = powerStatus == PowerBroadcastStatus.ResumeSuspend;

            Logger.Debug($"Changing power state: {powerStatus}, isSuspending: {isSuspending}, isResuming: {isResuming}");

            if (isSuspending)
            {
                _engine.Stop();
            }
            else if (isResuming)
            {
                var isRunning = _settingsService.GetSetting<bool>(Globals.SettingKeys.IsEngineRunning);

                if (isRunning)
                {
                    var delay = _settingsService.GetSetting<int>(Globals.SettingKeys.ResumeDelay);

                    Logger.Debug($"Delaying for {delay} seconds before starting the engine.");

                    Task.Run(async () =>
                    {
                        await Task.Delay(delay * 1000).ConfigureAwait(false);
                        _engine.Start();
                    }).ConfigureAwait(false);
                }
            }

            if (isSuspending || isResuming)
            {
                PowerEvents.OnPowerStateChanged(new PowerStateEventArgs(isSuspending, isResuming));
            }

            return base.OnPowerEvent(powerStatus);
        }*/

        private void LoadPlugins(IContainerProvider container)
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
                await _server.ConnectAsync().ConfigureAwait(false);
            }

            _reconnectTimer.Start();
        }

        private async void OnUpdateTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var interval = TimeSpan.FromHours(24).TotalMilliseconds;
            _updateTimer.Interval = interval;

            var updatesEnabled = _settingsService.GetSetting<bool>(Globals.SettingKeys.UpdateCheck);

            if (updatesEnabled)
            {
                var updateManifest = await _updateService.GetUpdateManifestAsync().ConfigureAwait(false);

                if (_updateService.IsUpdateAvailable(updateManifest))
                {
                    JsonResponse result;

                    do
                    {
                        result = await _server.SendMessageAsync(new JsonRequest(Globals.ApiResources.Updates, "updateavailable", updateManifest)).ConfigureAwait(false);

                        if (!result.IsConnected)
                        {
                            await Task.Delay(1000).ConfigureAwait(false);
                        }
                    } while (!result.IsConnected);
                }
            }

            _updateTimer.Start();
        }
    }
}