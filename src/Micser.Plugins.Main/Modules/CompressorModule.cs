using Microsoft.Extensions.Logging;
using Micser.Common.Audio;
using Micser.Common.Modules;
using Micser.Plugins.Main.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class CompressorModule : AudioModule
    {
        public CompressorModule(ILogger<CompressorModule> logger)
            : base(logger)
        {
            Amount = Defaults.Amount;
            Attack = Defaults.Attack;
            Knee = Defaults.Knee;
            MakeUpGain = Defaults.MakeUpGain;
            Ratio = Defaults.Ratio;
            Release = Defaults.Release;
            Threshold = Defaults.Threshold;
            Type = Defaults.Type;

            AddSampleProcessor(new CompressorSampleProcessor(this));
        }

        [SaveState]
        public float Amount { get; set; }

        [SaveState]
        public float Attack { get; set; }

        [SaveState]
        public float Knee { get; set; }

        [SaveState]
        public float MakeUpGain { get; set; }

        [SaveState]
        public float Ratio { get; set; }

        [SaveState]
        public float Release { get; set; }

        [SaveState]
        public float Threshold { get; set; }

        [SaveState]
        public CompressorType Type { get; set; }

        public static class Defaults
        {
            public const float Amount = 0f;
            public const float Attack = 0.01f;
            public const float Knee = 5f;
            public const float MakeUpGain = 0f;
            public const float Ratio = 2f;
            public const float Release = 0.01f;
            public const float Threshold = -10f;
            public const CompressorType Type = CompressorType.Upward;
        }
    }
}