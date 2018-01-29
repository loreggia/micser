using System.Threading.Tasks;
using NAudio.Wave.SampleProviders;

namespace Micser.Main.Audio
{
    public class SineGenerator : AudioChainLink
    {
        private readonly SampleToWaveProvider _waveProvider;
        private bool _isStopped;

        public SineGenerator()
        {
            var signalGenerator = new SignalGenerator
            {
                Frequency = 440,
                Type = SignalGeneratorType.Sin
            };
            _waveProvider = new SampleToWaveProvider(signalGenerator);
        }

        public void Start()
        {
            _isStopped = false;
            Task.Run(() => GeneratorThread());
        }

        public void Stop()
        {
            _isStopped = true;
        }

        private void GeneratorThread()
        {
            while (!_isStopped)
            {
                var buffer = new byte[1024];
                _waveProvider.Read(buffer, 0, 1024);
                OnDataAvailable(new AudioInputEventArgs(buffer, 1024));
            }
        }
    }
}