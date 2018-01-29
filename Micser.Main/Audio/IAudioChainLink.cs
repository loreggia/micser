using System;

namespace Micser.Main.Audio
{
    public interface IAudioChainLink
    {
        event EventHandler<AudioInputEventArgs> DataAvailable;

        event EventHandler InputChanged;

        IAudioChainLink Input { get; set; }
    }
}