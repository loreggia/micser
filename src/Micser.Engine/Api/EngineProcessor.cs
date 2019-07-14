using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Audio;

namespace Micser.Engine.Api
{
    [RequestProcessorName(Globals.ApiResources.Engine)]
    public class EngineProcessor : RequestProcessor
    {
        private readonly IAudioEngine _audioEngine;

        public EngineProcessor(IAudioEngine audioEngine)
        {
            _audioEngine = audioEngine;

            AddAction("start", _ => Start());
            AddAction("stop", _ => Stop());
            AddAction("restart", _ => Restart());
            AddAction("getstatus", _ => GetStatus());
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