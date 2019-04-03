using Micser.Common.Api;
using Micser.Engine.Audio;

namespace Micser.Engine.Api
{
    [RequestProcessorName("engine")]
    public class EngineProcessor : RequestProcessor
    {
        private readonly IAudioEngine _audioEngine;

        public EngineProcessor(IAudioEngine audioEngine)
        {
            _audioEngine = audioEngine;

            this["start"] = _ => Start();
            this["stop"] = _ => Stop();
            this["restart"] = _ => Restart();
            this["status"] = _ => GetStatus();
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