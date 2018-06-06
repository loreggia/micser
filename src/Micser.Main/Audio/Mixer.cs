namespace Micser.Main.Audio
{
    public class Mixer : AudioChainLink
    {
        public Mixer()
        {
        }

        public IAudioChainLink Input2 { get; set; }
    }
}