using Microsoft.Extensions.Logging;
using Micser.Common.Audio;
using Micser.Common.Modules;
using Micser.Plugins.Main.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class GainModule : AudioModule
    {
        public GainModule(ILogger<GainModule> logger)
            : base(logger)
        {
            Gain = Defaults.Gain;

            AddSampleProcessor(new GainSampleProcessor(this));
        }

        [SaveState]
        public float Gain { get; set; }

        public static class Defaults
        {
            public const float Gain = 0f;
        }
    }
}