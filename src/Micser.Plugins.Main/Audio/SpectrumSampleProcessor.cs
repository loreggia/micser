using CSCore;
using CSCore.DSP;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Api;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Audio
{
    public class SpectrumSampleProcessor : SampleProcessor
    {
        protected readonly SpectrumModule _module;
        protected int _currentSampleIndex;
        protected float[] _fftBuffer;
        protected SpectrumFftProvider _fftProvider;
        protected float[] _sampleBuffer;
        protected int _sampleBufferSize;
        protected SpectrumData.SpectrumValue[] _spectrumValueBuffer;

        public SpectrumSampleProcessor(SpectrumModule module)
        {
            _module = module;

            FftSize = FftSize.Fft4096;
            _sampleBufferSize = 4096;
        }

        public SpectrumData Data { get; protected set; }
        public FftSize FftSize { get; set; }

        public override void Process(WaveFormat waveFormat, ref float value)
        {
            _sampleBufferSize = waveFormat.SampleRate / 10 * waveFormat.Channels;

            if (_fftProvider == null
                || _fftProvider.Channels != waveFormat.Channels
                || _fftProvider.SampleRate != waveFormat.SampleRate
                || _fftProvider.FftSize != FftSize
                || _sampleBuffer == null
                || _sampleBuffer.Length != _sampleBufferSize)
            {
                _sampleBuffer = new float[_sampleBufferSize];
                _fftBuffer = new float[(int)FftSize];
                _spectrumValueBuffer = new SpectrumData.SpectrumValue[(int)FftSize / 2];
                _fftProvider = new SpectrumFftProvider(waveFormat.SampleRate, waveFormat.Channels, FftSize);
            }

            _sampleBuffer[_currentSampleIndex] = value;

            _currentSampleIndex++;

            if (_currentSampleIndex >= _sampleBuffer.Length)
            {
                _currentSampleIndex = 0;

                _fftProvider.Add(_sampleBuffer, _sampleBuffer.Length);
            }

            if (_fftProvider.IsNewDataAvailable)
            {
                _fftProvider.GetFftData(_fftBuffer);
                for (int i = 0; i < _spectrumValueBuffer.Length; i++)
                {
                    _spectrumValueBuffer[i].Frequency = _fftProvider.GetFftFrequency(i);
                    _spectrumValueBuffer[i].Value = _fftBuffer[i];
                }

                Data = new SpectrumData
                {
                    Values = _spectrumValueBuffer
                };
            }
        }
    }
}