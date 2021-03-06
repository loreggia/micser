﻿using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Audio;
using Micser.Common.Settings;
using System.Threading.Tasks;

namespace Micser.Engine.Api
{
    [RequestProcessorName(Globals.ApiResources.Engine)]
    public class EngineProcessor : RequestProcessor
    {
        private readonly IAudioEngine _audioEngine;
        private readonly ISettingsService _settingsService;

        public EngineProcessor(
            IAudioEngine audioEngine,
            ISettingsService settingsService)
        {
            _audioEngine = audioEngine;
            _settingsService = settingsService;

            AddAsyncAction("start", _ => Start());
            AddAsyncAction("stop", _ => Stop());
            AddAsyncAction("restart", _ => Restart());
            AddAction("getstatus", _ => GetStatus());
        }

        private bool GetStatus()
        {
            return _audioEngine.IsRunning;
        }

        private async Task<object> Restart()
        {
            _audioEngine.Stop();
            _audioEngine.Start();

            await _settingsService.SetSettingAsync(Globals.SettingKeys.IsEngineRunning, true).ConfigureAwait(false);

            return true;
        }

        private async Task<object> Start()
        {
            _audioEngine.Start();

            await _settingsService.SetSettingAsync(Globals.SettingKeys.IsEngineRunning, true).ConfigureAwait(false);

            return true;
        }

        private async Task<object> Stop()
        {
            _audioEngine.Stop();

            await _settingsService.SetSettingAsync(Globals.SettingKeys.IsEngineRunning, false).ConfigureAwait(false);

            return true;
        }
    }
}