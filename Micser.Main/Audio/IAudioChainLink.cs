using System;

namespace Micser.Main.Audio
{
    public interface IAudioChainLink
    {
        event EventHandler<AudioInputEventArgs> DataAvailable;

        IAudioChainLink Input { get; set; }
    }
}