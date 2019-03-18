using CSCore;
using Micser.Common.Modules;
using System;

namespace Micser.Engine.Infrastructure.Audio
{
    public interface IAudioModule : IDisposable
    {
        ModuleDto Description { get; }

        long Id { get; }

        void AddOutput(IAudioModule module);

        ModuleState GetState();

        void Initialize(ModuleDto description);

        void RemoveOutput(IAudioModule module);

        void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count);
    }
}