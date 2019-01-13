using Micser.Common.Modules;
using Micser.Engine.Infrastructure;
using System;
using System.Collections.Generic;

namespace Micser.Engine.Audio
{
    public interface IAudioEngine : IDisposable
    {
        ICollection<IAudioModule> Modules { get; }

        void AddModule(ModuleDescription module);

        void Start();

        void Stop();
    }
}