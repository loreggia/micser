using System;
using NAudio.Wave;

namespace Micser.Main.Audio
{
    public abstract class AudioChainLink : IAudioChainLink, IDisposable
    {
        private IAudioChainLink _input;

        public event EventHandler<AudioInputEventArgs> DataAvailable;

        public IAudioChainLink Input
        {
            get => _input;
            set
            {
                if (_input != null)
                {
                    _input.DataAvailable -= OnInputDataAvailable;
                }
                _input = value;
                if (_input != null)
                {
                    _input.DataAvailable += OnInputDataAvailable;
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
        }

        protected virtual void OnDataAvailable(AudioInputEventArgs e)
        {
            DataAvailable?.Invoke(this, e);
        }

        protected virtual void OnInputDataAvailable(object sender, AudioInputEventArgs e)
        {
            OnDataAvailable(e);
        }
    }
}