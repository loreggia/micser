using CSCore;
using Micser.Infrastructure.Models;
using System;

namespace Micser.Infrastructure.Modules
{
    public interface IModule : IDisposable
    {
        event EventHandler InputChanged;

        event EventHandler OutputChanged;

        ModuleDescription Description { get; }
        IModule Input { get; set; }
        IWaveSource Output { get; }

        ModuleState GetState();

        void Initialize(ModuleDescription description);
    }
}