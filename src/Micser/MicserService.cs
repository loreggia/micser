using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Micser.Common;
using Micser.Common.Audio;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Micser.Common.Updates;
using Timer = System.Timers.Timer;

namespace Micser
{
    public class MicserService : IHostedService
    {
        //private IApiServer _apiServer;

        private readonly ILogger<MicserService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Timer _updateTimer;
        private IAudioEngine _audioEngine;
        private ISettingsService _settingsService;
        private IUpdateService _updateService;

        static MicserService()
        {
            Directory.CreateDirectory(Globals.AppDataFolder);
        }

        public MicserService(IServiceProvider serviceProvider, ILogger<MicserService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _updateTimer = new Timer(1000) { AutoReset = false };
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting service");

            _audioEngine = _serviceProvider.GetRequiredService<IAudioEngine>();
            _updateService = _serviceProvider.GetRequiredService<IUpdateService>();

            _settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
            await _settingsService.LoadAsync().ConfigureAwait(false);

            if (_settingsService.GetSetting<bool>(Globals.SettingKeys.IsEngineRunning))
            {
                _audioEngine.StartAsync();
            }

            _updateTimer.Start();

            _logger.LogInformation("Service started");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping service");

            _updateTimer.Stop();

            _audioEngine.StopAsync();

            _logger.LogInformation("Service stopped");

            return Task.CompletedTask;
        }

        /*
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            var isSuspending = powerStatus == PowerBroadcastStatus.Suspend;
            var isResuming = powerStatus == PowerBroadcastStatus.ResumeSuspend;

            _logger.Debug($"Changing power state: {powerStatus}, isSuspending: {isSuspending}, isResuming: {isResuming}");

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
        }
        */

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
                    //ApiResponse result;

                    //do
                    //{
                    //    result = await _apiClient.SendMessageAsync(Globals.ApiResources.Updates, "updateavailable", updateManifest).ConfigureAwait(false);

                    //    if (!result.IsSuccess)
                    //    {
                    //        await Task.Delay(1000).ConfigureAwait(false);
                    //    }
                    //} while (!result.IsSuccess);
                }
            }

            _updateTimer.Start();
        }
    }
}