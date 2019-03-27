using CSCore;
using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Extensions;
using Micser.Plugins.Main.Modules;
using System;

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

        public void Process(WaveFormat waveFormat, ref float value)
        {
            if (_module.Type == CompressorType.Upward)
            {
                ProcessUpward(waveFormat, ref value);
            }
            else
            {
                ProcessDownward(waveFormat, ref value);
            }

            MathExtensions.Clamp(ref value, -1f, 1f);
        }

        public void ProcessDownward(WaveFormat waveFormat, ref float value)
        {
        }

        public void ProcessUpward(WaveFormat waveFormat, ref float value)
        {
            var threshold = _module.Threshold;
            var abs = Math.Abs(value);

            if (abs < threshold)
            {
                var amount = _module.Amount;
                var ratio = _module.Ratio;

                // WA: plot y=x; y=(x+0.5)/2;y=0.5; from x=-1 to 1; from y=-1 to 1
                var newValue = abs + ((abs + threshold) / ratio - abs) * amount;
                value = Math.Sign(value) * newValue;
            }
        }
    }
}