using CSCore;
using CSCore.DSP;
using Micser.Common.Api;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Api;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Audio
{
    public class SpectrumSampleProcessor : SampleProcessor
    {
        protected readonly IApiEndPoint _apiEndPoint;
        protected readonly SpectrumModule _module;
        protected int _currentSampleIndex;
        protected float[] _fftBuffer;
        protected SpectrumFftProvider _fftProvider;
        protected float[] _sampleBuffer;

        public SpectrumSampleProcessor(SpectrumModule module, IApiEndPoint apiEndPoint)
        {
            _apiEndPoint = apiEndPoint;
            _module = module;

            FftSize = FftSize.Fft4096;
            ChannelBufferSize = 512;
        }

        public int ChannelBufferSize { get; set; }
        public FftSize FftSize { get; set; }

        public override void Process(WaveFormat waveFormat, ref float value)
        {
            if (_fftProvider == null
                || _fftProvider.Channels != waveFormat.Channels
                || _fftProvider.SampleRate != waveFormat.SampleRate
                || _fftProvider.FftSize != FftSize
                || _sampleBuffer == null
                || _sampleBuffer.Length != (ChannelBufferSize * waveFormat.Channels))
            {
                _sampleBuffer = new float[ChannelBufferSize * waveFormat.Channels];
                _fftBuffer = new float[(int)FftSize];
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
                _apiEndPoint.SendMessageAsync(new JsonRequest("spectrum", null, new SpectrumData()));
            }
        }
    }
}