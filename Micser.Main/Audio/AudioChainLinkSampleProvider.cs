using NAudio.Wave;

namespace Micser.Main.Audio
{
    public class AudioChainLinkSampleProvider : AudioChainLink, ISampleProvider
    {
        public virtual WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
    }
}