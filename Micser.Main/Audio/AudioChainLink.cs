using System;

namespace Micser.Main.Audio
{
    public class AudioChainLink : IAudioChainLink, IDisposable
    {
        public AudioChainLink()
        {
            Volume = 1f;
        }

        public IAudioChainLink Input { get; set; }
        public float Volume { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var read = ReadInternal(buffer, offset, count);
            ApplyVolume(buffer, offset, read);
            return read;
        }

        protected void ApplyVolume(float[] buffer, int offset, int count)
        {
            if (Volume == 1f)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] *= Volume;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            Input = null;
        }

        protected virtual int ReadInternal(float[] buffer, int offset, int count)
        {
            return Input?.Read(buffer, offset, count) ?? 0;
        }
    }
}