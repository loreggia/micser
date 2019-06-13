using CSCore;
using Micser.Common.Extensions;

namespace Micser.Engine.Infrastructure.Audio
{
    /// <summary>
    /// Multiplies each sample value by the module's <see cref="AudioModule.Volume"/> property.
    /// </summary>
    public class VolumeSampleProcessor : ISampleProcessor
    {
        private readonly IAudioModule _module;

        /// <summary>
        /// Creates an instance of the <see cref="VolumeSampleProcessor"/> class.
        /// </summary>
        /// <param name="module">Reference to the module this processor belongs to.</param>
        public VolumeSampleProcessor(IAudioModule module)
        {
            _module = module;
            IsEnabled = true;
            Priority = int.MaxValue;
        }

        /// <inheritdoc />
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Defaults to int.MaxValue.
        /// </summary>
        public int Priority { get; set; }

        /// <inheritdoc />
        public void Process(WaveFormat waveFormat, float[] channelSamples)
        {
            for (int c = 0; c < waveFormat.Channels; c++)
            {
                channelSamples[c] *= _module.Volume;
                MathExtensions.Clamp(ref channelSamples[c], -1f, 1f);
            }
        }
    }
}