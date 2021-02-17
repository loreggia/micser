using System;
using Microsoft.Extensions.Logging;
using Micser.Common.Audio;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using Micser.Plugins.Main.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class PitchModule : AudioModule
    {
        public PitchModule(ILogger<PitchModule> logger)
            : base(logger)
        {
            AddSampleProcessor(new PitchSampleProcessor(this));
        }

        public int FftSize { get; set; }
        public int Oversampling { get; set; }

        [SaveState(Defaults.Pitch)]
        public float Pitch { get; set; }

        public float PitchFactor { get; set; }

        [SaveState(Defaults.Quality)]
        public int Quality { get; set; }

        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            var quality = Quality;
            MathExtensions.Clamp(ref quality, 1, 10);
            var pitch = Pitch;
            MathExtensions.Clamp(ref pitch, -1f, 1f);

            // [256..4096] -> map quality [1..10] to [8..12]
            var qualityFactor = MathExtensions.InverseLerp(1f, 10f, quality);
            var fftQualityPow = (int)MathExtensions.Lerp(8, 12, qualityFactor);
            FftSize = (int)Math.Pow(2, fftQualityPow);
            Oversampling = (int)MathExtensions.Lerp(4f, 8f, qualityFactor);

            PitchFactor = pitch < 0f
                ? MathExtensions.Lerp(0.5f, 1f, pitch + 1)
                : MathExtensions.Lerp(1f, 2f, pitch);
        }

        public static class Defaults
        {
            public const float Pitch = 1f;
            public const int Quality = 4;
        }
    }
}