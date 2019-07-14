using Microsoft.Win32;
using Micser.Common.Audio;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Audio;
using System;
using System.Threading.Tasks;

namespace Micser.Plugins.Main.Modules
{
    public class RestartEngineModule : AudioModule
    {
        private readonly IAudioEngine _audioEngine;

        public RestartEngineModule(IAudioEngine audioEngine)
        {
            _audioEngine = audioEngine;
            SystemEvents.PowerModeChanged += PowerModeChanged;
        }

        [SaveState(5f)]
        public float Delay { get; set; }

        public override ModuleState GetState()
        {
            return base.GetState();
        }

        public override void SetState(ModuleState state)
        {
            base.SetState(state);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                SystemEvents.PowerModeChanged -= PowerModeChanged;
            }
        }

        private async void PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode != PowerModes.Resume)
            {
                return;
            }

            await Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(Delay));

                if (!_audioEngine.IsRunning)
                {
                    return;
                }

                _audioEngine.Stop();
                while (_audioEngine.IsRunning)
                {
                    await Task.Delay(100);
                }
                _audioEngine.Start();
            });
        }
    }
}