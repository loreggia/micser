using System;
using CSCore;

namespace Micser.Main.Audio
{
    public interface IAudioChainLink
    {
        IAudioChainLink Input { get; set; }
        IWaveSource Output { get; }
        event EventHandler InputChanged;
        event EventHandler OutputChanged;
    }
}