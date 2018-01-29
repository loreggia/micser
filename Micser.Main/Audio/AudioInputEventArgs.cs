namespace Micser.Main.Audio
{
    public class AudioInputEventArgs
    {
        public AudioInputEventArgs(float[] buffer, int count)
        {
            Buffer = buffer;
            Count = count;
        }

        public float[] Buffer { get; set; }
        public int Count { get; set; }
    }
}