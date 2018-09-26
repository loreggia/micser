using CSCore;
using Micser.Shared.Models;
using System;

namespace Micser.Engine.Audio
{
    public interface IAudioModule : IDisposable
    {
        event EventHandler InputChanged;

        event EventHandler OutputChanged;

        IAudioModule Input { get; set; }
        IWaveSource Output { get; }

        string GetState();

        void Initialize(ModuleDescription state);
    }
}