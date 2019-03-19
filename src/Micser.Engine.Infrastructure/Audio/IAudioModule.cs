using CSCore;
using Micser.Common.Modules;
using Micser.Common.Widgets;
using System;

namespace Micser.Engine.Infrastructure.Audio
{
    public interface IAudioModule : IDisposable
    {
        long Id { get; }

        void AddOutput(IAudioModule module);

        void Initialize(ModuleState state);

        void RemoveOutput(IAudioModule module);

        ModuleState UpdateModuleState(WidgetState widgetState);

        void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count);
    }
}