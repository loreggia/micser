using CSCore.DSP;

namespace Micser.Plugins.Main.Audio
{
    public class SpectrumFftProvider : FftProvider
    {
        public SpectrumFftProvider(int sampleRate, int channels, FftSize fftSize)
            : base(channels, fftSize)
        {
            Channels = channels;
            SampleRate = sampleRate;
        }

        public int Channels { get; }
        public int SampleRate { get; }

        public int GetFftBandIndex(float frequency)
        {
            var fftSize = (int)FftSize;
            return (int)(frequency / SampleRate * fftSize);
        }

        public float GetFftFrequency(int index)
        {
            var fftSize = (int)FftSize;
            return index * SampleRate / fftSize;
        }
    }
}