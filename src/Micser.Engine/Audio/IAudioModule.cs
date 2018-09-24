using CSCore;
using System;

namespace Micser.Engine.Audio
{
    public interface IAudioModule
    {
        event EventHandler InputChanged;

        event EventHandler OutputChanged;

        IAudioModule Input { get; set; }
        IWaveSource Output { get; }
    }
}