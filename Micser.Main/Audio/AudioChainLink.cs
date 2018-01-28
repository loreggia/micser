using System;
using NAudio.Wave;

namespace Micser.Main.Audio
{
    public abstract class AudioChainLink : IAudioChainLink, IDisposable
    {
        public event EventHandler<WaveInEventArgs> DataAvailable;

        public IAudioChainLink Input { get; set; }
        public IAudioChainLink Output { get; protected set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        protected virtual void OnDataAvailable(WaveInEventArgs e)
        {
            DataAvailable?.Invoke(this, e);
        }
    }
}