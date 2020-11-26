using CSCore;
using CSCore.DSP;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Api;

namespace Micser.Plugins.Main.Audio
{
    public class SpectrumSampleProcessor : SampleProcessor
    {
        private float[] _fftBuffer;
        private SpectrumFftProvider _fftProvider;
        private SpectrumValue[] _spectrumValueBuffer;

        public SpectrumSampleProcessor()
        {
            FftSize = FftSize.Fft4096;
        }

        public FftSize FftSize { get; }

        public SpectrumData GetFftData()
        {
            if (_fftBuffer == null)
            {
                return null;
            }

            _fftProvider.GetFftData(_fftBuffer);

            for (int i = 0; i < _spectrumValueBuffer.Length; i++)
            {
                _spectrumValueBuffer[i].Frequency = _fftProvider.GetFftFrequency(i);
                _spectrumValueBuffer[i].Value = _fftBuffer[i];
            }

            return new SpectrumData
            {
                Values = { _spectrumValueBuffer }
            };
        }

        public override void Process(WaveFormat waveFormat, float[] channelSamples)
        {
            if (_fftProvider == null
                || _fftProvider.Channels != waveFormat.Channels
                || _fftProvider.SampleRate != waveFormat.SampleRate
                || _fftProvider.FftSize != FftSize)
            {
                _fftBuffer = new float[(int)FftSize];
                _spectrumValueBuffer = new SpectrumValue[(int)FftSize / 2];
                _fftProvider = new SpectrumFftProvider(waveFormat.SampleRate, waveFormat.Channels, FftSize);
            }

            _fftProvider.Add(channelSamples, waveFormat.Channels);
        }
    }
}