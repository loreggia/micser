using System;

namespace Micser.Main.Audio
{
    public abstract class AudioChainLink : IAudioChainLink, IDisposable
    {
        private IAudioChainLink _input;

        public event EventHandler<AudioInputEventArgs> DataAvailable;

        public event EventHandler InputChanged;

        public IAudioChainLink Input
        {
            get => _input;
            set
            {
                if (_input != null)
                {
                    _input.DataAvailable -= OnInputDataAvailable;
                }
                if (_input != value)
                {
                    _input = value;
                    if (_input != null)
                    {
                        _input.DataAvailable += OnInputDataAvailable;
                    }
                    OnInputChanged();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _input = null;
        }

        protected virtual void OnDataAvailable(AudioInputEventArgs e)
        {
            DataAvailable?.Invoke(this, e);
        }

        protected virtual void OnInputChanged()
        {
            InputChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnInputDataAvailable(object sender, AudioInputEventArgs e)
        {
            OnDataAvailable(e);
        }
    }
}