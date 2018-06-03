using CSCore;

namespace Micser.Main.Audio
{
    public interface IAudioChainLink
    {
        IAudioChainLink Input { get; set; }
        IWaveSource Output { get; set; }
        float Volume { get; set; }
    }
}