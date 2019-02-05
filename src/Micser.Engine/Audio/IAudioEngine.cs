using System;

namespace Micser.Engine.Audio
{
    public interface IAudioEngine : IDisposable
    {
        bool IsRunning { get; }

        void AddModule(long id);

        void Start();

        void Stop();

        void UpdateModule(long id);
    }
}