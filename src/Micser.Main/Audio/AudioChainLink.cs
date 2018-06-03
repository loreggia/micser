using System;
using CSCore;

namespace Micser.Main.Audio
{
    public abstract class AudioChainLink : IAudioChainLink, IDisposable
    {
        private float[] _buffer;

        protected AudioChainLink()
        {
            Volume = 1f;
        }

        public event AudioDataEventHandler DataAvailable;

        public IAudioChainLink Input { get; set; }
        public IWaveSource Output { get; set; }
        public float Volume { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void ApplyVolume(float[] buffer, int count)
        {
            if (Volume == 1f)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                buffer[i] *= Volume;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            Input = null;
        }

        protected void EnsureBuffer(int count)
        {
            if ((_buffer?.Length ?? 0) < count)
            {
                _buffer = new float[count];
            }
        }

        protected virtual void OnDataAvailable(float[] buffer, int count, int channelCount)
        {
            DataAvailable?.Invoke(this, new AudioDataEventArgs(buffer, count, channelCount));
        }

        protected virtual void OnInputDataAvailable(object sender, AudioDataEventArgs e)
        {
            // default implementation applies volume and sends the data to the next link
            EnsureBuffer(e.Count);

            Array.Copy(e.Buffer, _buffer, e.Count);

            ApplyVolume(_buffer, e.Count);

            OnDataAvailable(_buffer, e.Count, e.ChannelCount);
        }
    }
}