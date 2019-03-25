using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Extensions;

namespace Micser.Plugins.Main.Audio
{
    public class CompressorSampleProcessor : ISampleProcessor
    {
        public CompressorSampleProcessor()
        {
            IsEnabled = true;
            Priority = 50;
        }

        public bool IsEnabled { get; set; }
        public int Priority { get; set; }

        public void Process(ref float value)
        {
            value *= 10f;
            MathExtensions.Clamp(ref value, -1f, 1f);
        }
    }
}