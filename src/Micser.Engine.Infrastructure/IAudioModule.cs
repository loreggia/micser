using CSCore;
using Micser.Common;
using Micser.Common.Modules;
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

        StateDictionary GetState();

        void Initialize(ModuleDescription description);
    }
}