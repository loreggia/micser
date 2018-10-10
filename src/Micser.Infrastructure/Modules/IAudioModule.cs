using System;
using CSCore;
using Micser.Infrastructure.Models;

namespace Micser.Infrastructure.Modules
{
    public interface IAudioModule : IDisposable
    {
        event EventHandler InputChanged;

        event EventHandler OutputChanged;

        Module Description { get; set; }
        IAudioModule Input { get; set; }
        IWaveSource Output { get; }

        string GetState();

        void Initialize(Module state);
    }
}