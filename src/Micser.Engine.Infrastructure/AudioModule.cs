using CSCore;
using Micser.Common.Modules;
using NLog;
using System;

namespace Micser.Engine.Infrastructure
{
    public abstract class AudioModule : IAudioModule
    {
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private IAudioModule _input;
        private IWaveSource _output;

        public event EventHandler InputChanged;

        public event EventHandler OutputChanged;

        public ModuleDescription Description { get; set; }

        public IAudioModule Input
        {
            get => _input;
            set
            {
                if (_input == value)
                {
                    return;
                }

                if (_input != null)
                {
                    _input.OutputChanged -= OnInputOutputChanged;
                }

                _input = value;

                if (value != null)
                {
                    _input.OutputChanged += OnInputOutputChanged;
                }

                OnInputChanged();
            }
        }

        public IWaveSource Output
        {
            get => _output;
            protected set
            {
                if (_output == value)
                {
                    return;
                }

                _output = value;
                OnOutputChanged();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract IModuleState GetState();

        public virtual void Initialize(ModuleDescription description)
        {
            Description = description;
        }

        protected virtual void Dispose(bool disposing)
        {
            Input = null;
        }

        protected virtual void OnInputChanged()
        {
            InputChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnInputOutputChanged(object sender, EventArgs e)
        {
        }

        protected virtual void OnOutputChanged()
        {
            OutputChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}