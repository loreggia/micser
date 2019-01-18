using System;
using CSCore;
using Micser.Common.Modules;

namespace Micser.Engine.Infrastructure
{
    public interface IAudioModule : IDisposable
    {
        ModuleDescription Description { get; }
        IAudioModule Input { get; set; }
        IWaveSource Output { get; }
        event EventHandler InputChanged;

        event EventHandler OutputChanged;

        IModuleState GetState();

        void Initialize(ModuleDescription description);
    }
}