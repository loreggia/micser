using System;

namespace Micser.Engine.Infrastructure
{
    public interface IServer : IDisposable
    {
        void Start();

        void Stop();
    }
}