using NAudio.Wave;

namespace Micser.Main.Audio
{
    public class AudioInputEventArgs
    {
        public AudioInputEventArgs(WaveInEventArgs e)
            : this(e.Buffer, e.BytesRecorded)
        {
        }

        public AudioInputEventArgs(byte[] buffer, int count)
        {
            Buffer = buffer;
            Count = count;
        }

        public byte[] Buffer { get; set; }
        public int Count { get; set; }
    }
}