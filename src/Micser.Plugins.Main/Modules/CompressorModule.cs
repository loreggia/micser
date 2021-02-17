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
            AddSampleProcessor(new CompressorSampleProcessor(this));
        }

        [SaveState(Defaults.Amount)]
        public float Amount { get; set; }

        [SaveState(Defaults.Attack)]
        public float Attack { get; set; }

        [SaveState(Defaults.Knee)]
        public float Knee { get; set; }

        [SaveState(Defaults.MakeUpGain)]
        public float MakeUpGain { get; set; }

        [SaveState(Defaults.Ratio)]
        public float Ratio { get; set; }

        [SaveState(Defaults.Release)]
        public float Release { get; set; }

        [SaveState(Defaults.Threshold)]
        public float Threshold { get; set; }

        [SaveState(Defaults.Type)]
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