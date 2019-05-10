using CSCore;
using Micser.Engine.Infrastructure.Extensions;

namespace Micser.Engine.Infrastructure.Audio
{
    /// <summary>
    /// Multiplies each sample value by the module's <see cref="AudioModule.Volume"/> property.
    /// </summary>
    public class VolumeSampleProcessor : ISampleProcessor
    {
        private readonly IAudioModule _module;

        public VolumeSampleProcessor(IAudioModule module)
        {
            _module = module;
            IsEnabled = true;
            Priority = int.MaxValue;
        }

        public bool IsEnabled { get; set; }

        /// <summary>
        /// Defaults to int.MaxValue.
        /// </summary>
        public int Priority { get; set; }

        public void Process(WaveFormat waveFormat, ref float value)
        {
            value *= _module.Volume;
            MathExtensions.Clamp(ref value, -1f, 1f);
        }
    }
}