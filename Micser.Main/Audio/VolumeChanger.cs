using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Micser.Main.Audio
{
    public class VolumeChanger : AudioChainLink
    {
        private readonly BufferedWaveProvider _buffer;
        private readonly SampleChannel _sampleChannel;
        private readonly SampleToWaveProvider _waveProvider;

        public VolumeChanger(WaveFormat waveFormat)
        {
            _buffer = new BufferedWaveProvider(waveFormat);
            _sampleChannel = new SampleChannel(_buffer);
            _waveProvider = new SampleToWaveProvider(_sampleChannel);
        }

        public float Volume
        {
            get => _sampleChannel.Volume;
            set => _sampleChannel.Volume = value;
        }

        protected override void OnInputChanged()
        {
            base.OnInputChanged();
        }

        protected override void OnInputDataAvailable(object sender, AudioInputEventArgs e)
        {
            _buffer.AddSamples(e.Buffer, 0, e.Count);
            var outputBuffer = new byte[e.Count];
            _waveProvider.Read(outputBuffer, 0, e.Count);
            OnDataAvailable(new AudioInputEventArgs(outputBuffer, e.Count));
        }
    }
}