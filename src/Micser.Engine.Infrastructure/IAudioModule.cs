using CSCore;
using Micser.Infrastructure.Models;
using System;

namespace Micser.Engine.Infrastructure
{
    public interface IAudioModule : IDisposable
    {
        event EventHandler InputChanged;

        event EventHandler OutputChanged;

        ModuleDescription Description { get; }
        IAudioModule Input { get; set; }
        IWaveSource Output { get; }

        ModuleState GetState();

        void Initialize(ModuleDescription description);
    }
}