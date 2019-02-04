using CSCore;
using Micser.Common.Modules;
using System;

namespace Micser.Engine.Infrastructure
{
    public interface IAudioModule : IDisposable
    {
        event EventHandler InputChanged;

        event EventHandler OutputChanged;

        ModuleDto Description { get; }
        IAudioModule Input { get; set; }
        IWaveSource Output { get; }

        ModuleState GetState();

        void Initialize(ModuleDto description);
    }
}