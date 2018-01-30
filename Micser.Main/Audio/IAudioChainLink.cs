using NAudio.Wave;

namespace Micser.Main.Audio
{
    public interface IAudioChainLink
    {
        IAudioChainLink Input { get; set; }
        float Volume { get; set; }

        int Read(float[] buffer, int offset, int count);
    }
}