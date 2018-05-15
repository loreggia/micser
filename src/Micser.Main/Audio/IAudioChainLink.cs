namespace Micser.Main.Audio
{
    public interface IAudioChainLink
    {
        event AudioDataEventHandler DataAvailable;

        IAudioChainLink Input { get; set; }
        float Volume { get; set; }
    }
}