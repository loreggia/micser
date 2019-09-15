using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class PitchModule : AudioModule
    {
        public PitchModule()
        {
            AddSampleProcessor(new PitchSampleProcessor(this));
        }

        [SaveState(Defaults.FftSize)]
        public int FftSize { get; set; }

        [SaveState(Defaults.Pitch)]
        public float Pitch { get; set; }

        [SaveState(Defaults.Quality)]
        public int Quality { get; set; }

        public class Defaults
        {
            public const int FftSize = 1024;
            public const float Pitch = 1f;
            public const int Quality = 4;
        }
    }
}