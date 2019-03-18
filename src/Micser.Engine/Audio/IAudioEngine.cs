using System;

namespace Micser.Engine.Audio
{
    public interface IAudioEngine : IDisposable
    {
        bool IsRunning { get; }

        void AddConnection(long id);

        void AddModule(long id);

        void DeleteModule(long id);

        void RemoveConnection(long id);

        void Start();

        void Stop();

        void UpdateModule(long id);
    }
}