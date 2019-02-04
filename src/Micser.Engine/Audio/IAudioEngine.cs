using System;

namespace Micser.Engine.Audio
{
    public interface IAudioEngine : IDisposable
    {
        void AddModule(long id);

        void Start();

        void Stop();

        void UpdateModule(long id);
    }
}