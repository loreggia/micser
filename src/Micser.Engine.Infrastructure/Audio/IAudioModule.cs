using CSCore;
using Micser.Common.Modules;
using System;

namespace Micser.Engine.Infrastructure.Audio
{
    /// <summary>
    /// Describes an audio module that can be used by the audio engine.
    /// </summary>
    public interface IAudioModule : IDisposable
    {
        long Id { get; }
        bool IsMuted { get; set; }
        bool UseSystemVolume { get; set; }
        float Volume { get; set; }

        void AddOutput(IAudioModule module);

        ModuleState GetState();

        void RemoveOutput(IAudioModule module);

        void SetState(ModuleState state);

        void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count);
    }
}