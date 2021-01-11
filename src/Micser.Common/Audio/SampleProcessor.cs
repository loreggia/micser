using CSCore;

namespace Micser.Common.Audio
{
    /// <summary>
    /// Abstract base class implementing the <see cref="ISampleProcessor"/> interface.
    /// </summary>
    public abstract class SampleProcessor : ISampleProcessor
    {
        protected SampleProcessor()
        {
            IsEnabled = true;
            Priority = 0;
        }

        /// <inheritdoc />
        public bool IsEnabled { get; set; }

        /// <inheritdoc />
        public int Priority { get; set; }

        /// <inheritdoc />
        public abstract void Process(WaveFormat waveFormat, float[] channelSamples);
    }
}