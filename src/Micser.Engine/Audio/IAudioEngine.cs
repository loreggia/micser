using System;
using System.Collections.Generic;
using Micser.Engine.Infrastructure;
using Unity;

namespace Micser.Engine.Audio
{
    public interface IAudioEngine : IDisposable
    {
        ICollection<IModule> Modules { get; }

        void Start(IUnityContainer container);

        void Stop();
    }
}