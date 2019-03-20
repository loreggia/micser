using CSCore;
using Micser.Common.Modules;
using System;

namespace Micser.Engine.Infrastructure.Audio
{
    public interface IAudioModule : IDisposable
    {
        long Id { get; }

        void AddOutput(IAudioModule module);

        ModuleState GetState();

        void RemoveOutput(IAudioModule module);

        void SetState(ModuleState state);

        void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count);
    }
}