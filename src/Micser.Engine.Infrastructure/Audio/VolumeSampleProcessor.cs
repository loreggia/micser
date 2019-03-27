using CSCore;
using Micser.Engine.Infrastructure.Extensions;

namespace Micser.Engine.Infrastructure.Audio
{
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
        public int Priority { get; set; }

        public void Process(WaveFormat waveFormat, ref float value)
        {
            value *= _module.Volume;
            MathExtensions.Clamp(ref value, -1f, 1f);
        }
    }
}