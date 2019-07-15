﻿using Micser.Common.Audio;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.Audio;
using NLog;
using System;
using System.Threading.Tasks;

namespace Micser.Plugins.Main.Modules
{
    public class RestartEngineModule : AudioModule
    {
        private readonly IAudioEngine _audioEngine;
        private readonly ILogger _logger;

        public RestartEngineModule(IAudioEngine audioEngine, ILogger logger)
        {
            _audioEngine = audioEngine;
            _logger = logger;
            PowerEvents.PowerStateChanged += PowerStateChanged;
        }

        [SaveState(5f)]
        public float Delay { get; set; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
            }
        }

        private async void PowerStateChanged(object sender, PowerStateEventArgs e)
        {
            if (!e.IsResuming)
            {
                return;
            }

            await Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(Delay));

                _logger.Debug($"Audio engine state: {_audioEngine.IsRunning}");

                if (!_audioEngine.IsRunning)
                {
                    return;
                }

                _logger.Debug("Restarting audio engine");

                _audioEngine.Stop();
                while (_audioEngine.IsRunning)
                {
                    await Task.Delay(100);
                }
                _audioEngine.Start();

                _logger.Debug("Restarted audio engine");
            });
        }
    }
}