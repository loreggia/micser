using System;

namespace Micser.Main.Audio
{
    public delegate void AudioDataEventHandler(object sender, AudioDataEventArgs e);

    public class AudioDataEventArgs : EventArgs
    {
        public AudioDataEventArgs(float[] buffer, int count, int channelCount)
        {
            Buffer = buffer;
            Count = count;
            ChannelCount = channelCount;
        }

        public float[] Buffer { get; set; }
        public int ChannelCount { get; set; }
        public int Count { get; set; }
    }
}