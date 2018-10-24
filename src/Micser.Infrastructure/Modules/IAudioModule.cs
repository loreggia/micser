using CSCore;
using Micser.Infrastructure.Models;
using System;

namespace Micser.Infrastructure.Modules
{
    public interface IAudioModule : IDisposable
    {
        Type WidgetType { get; }

        event EventHandler InputChanged;

        event EventHandler OutputChanged;

        Module Description { get; set; }
        IAudioModule Input { get; set; }
        IWaveSource Output { get; }

        string GetState();

        void Initialize(Module state);
    }
}