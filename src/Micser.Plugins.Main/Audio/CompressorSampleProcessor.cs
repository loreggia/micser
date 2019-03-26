using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Extensions;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Audio
{
    public class CompressorSampleProcessor : ISampleProcessor
    {
        private readonly CompressorModule _module;

        public CompressorSampleProcessor(CompressorModule module)
        {
            _module = module;
            IsEnabled = true;
            Priority = 50;
        }

        public bool IsEnabled { get; set; }
        public int Priority { get; set; }

        public void Process(ref float value)
        {
            if (_module.Type == CompressorType.Upward)
            {
                ProcessUpward(ref value);
            }
            else
            {
                ProcessDownward(ref value);
            }

            MathExtensions.Clamp(ref value, -1f, 1f);
        }

        public void ProcessDownward(ref float value)
        {
        }

        public void ProcessUpward(ref float value)
        {
        }
    }
}