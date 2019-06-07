using Micser.Common;
using Micser.Common.Api;
using Micser.Engine.Audio;

namespace Micser.Engine.Api
{
    [RequestProcessorName(Globals.ApiResources.Engine)]
    public class EngineProcessor : RequestProcessor
    {
        private readonly IAudioEngine _audioEngine;

        public EngineProcessor(IAudioEngine audioEngine)
        {
            _audioEngine = audioEngine;

            this["start"] = _ => Start();
            this["stop"] = _ => Stop();
            this["restart"] = _ => Restart();
            this["getstatus"] = _ => GetStatus();
        }

        private bool GetStatus()
        {
            return _audioEngine.IsRunning;
        }

        private bool Restart()
        {
            _audioEngine.Stop();
            _audioEngine.Start();
            return true;
        }

        private bool Start()
        {
            _audioEngine.Start();
            return true;
        }

        private bool Stop()
        {
            _audioEngine.Stop();
            return true;
        }
    }
}