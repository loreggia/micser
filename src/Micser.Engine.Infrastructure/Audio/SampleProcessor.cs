using CSCore;

namespace Micser.Engine.Infrastructure.Audio
{
    public abstract class SampleProcessor : ISampleProcessor
    {
        protected SampleProcessor()
        {
            IsEnabled = true;
            Priority = 0;
        }

        public bool IsEnabled { get; set; }
        public int Priority { get; set; }

        public abstract void Process(WaveFormat waveFormat, float[] channelSamples);
    }
}