namespace Micser.Main.Audio
{
    public interface IAudioChainLink
    {
        IAudioChainLink Input { get; set; }
        IAudioChainLink Output { get; set; }
    }
}