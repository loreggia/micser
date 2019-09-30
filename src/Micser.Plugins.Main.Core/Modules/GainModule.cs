using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class GainModule : AudioModule
    {
        public GainModule()
        {
            AddSampleProcessor(new GainSampleProcessor(this));
        }

        [SaveState(0f)]
        public float Gain { get; set; }
    }
}