using CSCore;
using Micser.Common;
using Micser.Common.Modules;
using Micser.Common.Widgets;
using NLog;
using System;
using System.Collections.Generic;

namespace Micser.Engine.Infrastructure.Audio
{
    public abstract class AudioModule : IAudioModule, IDisposable
    {
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly IList<IAudioModule> _outputs;

        protected AudioModule(long id)
        {
            Id = id;
            _outputs = new List<IAudioModule>(2);
        }

        ~AudioModule()
        {
            Dispose(false);
        }

        public virtual long Id { get; }

        public virtual float Volume { get; set; } = 1f;

        public virtual void AddOutput(IAudioModule module)
        {
            if (_outputs.Contains(module))
            {
                return;
            }

            _outputs.Add(module);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual ModuleState GetState()
        {
        }

        public virtual void Initialize(ModuleState state)
        {
        }

        public virtual void RemoveOutput(IAudioModule module)
        {
            if (_outputs.Contains(module))
            {
                _outputs.Remove(module);
            }
        }

        public virtual ModuleState SetState(WidgetState widgetState)
        {
            return new ModuleState
            {
                Data = new StateDictionary(widgetState.Data),
                Volume = widgetState.Volume,
                IsMuted = widgetState.IsMuted
            };
        }

        public virtual void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count)
        {
            foreach (var module in _outputs)
            {
                module.Write(source, waveFormat, buffer, offset, count);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _outputs.Clear();
            }
        }
    }
}