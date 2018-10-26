using Micser.Infrastructure.Modules;
using System;
using System.Collections.Generic;
using Unity;

namespace Micser.Infrastructure.Audio
{
    public interface IAudioEngine : IDisposable
    {
        ICollection<IModule> Modules { get; }

        void Start(IUnityContainer container);

        void Stop();
    }
}