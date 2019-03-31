using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Extensions;
using Micser.Plugins.Main.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class CompressorModule : AudioModule
    {
        private float _ratio;

        public CompressorModule(long id)
            : base(id)
        {
            AddSampleProcessor(new CompressorSampleProcessor(this));
        }

        [SaveState(Defaults.Amount)]
        public float Amount { get; set; }

        [SaveState(Defaults.Attack)]
        public float Attack { get; set; }

        [SaveState(Defaults.Knee)]
        public float Knee { get; set; }

        [SaveState(Defaults.Ratio)]
        public float Ratio
        {
            get => _ratio;
            set
            {
                MathExtensions.Clamp(ref value, -60f, 60f);
                _ratio = value;
            }
        }

        [SaveState(Defaults.Release)]
        public float Release { get; set; }

        [SaveState(Defaults.Threshold)]
        public float Threshold { get; set; }

        [SaveState(Defaults.Type)]
        public CompressorType Type { get; set; }

        public class Defaults
        {
            public const float Amount = 1f;
            public const float Attack = 1f;
            public const float Knee = 5f;
            public const float Ratio = 2f;
            public const float Release = 10f;
            public const float Threshold = -20f;
            public const CompressorType Type = CompressorType.Upward;
        }
    }
}