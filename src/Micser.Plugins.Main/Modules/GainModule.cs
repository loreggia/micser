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
            AddSampleProcessor(new GainSampleProcessor(this));
        }

        [SaveState(0f)]
        public float Gain { get; set; }
    }
}