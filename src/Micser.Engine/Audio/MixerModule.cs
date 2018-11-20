using Micser.Engine.Infrastructure;
using Micser.Infrastructure.Models;

namespace Micser.Engine.Audio
{
    public class MixerModule : AudioModule
    {
        public MixerModule()
        {
        }

        public IAudioModule Input2 { get; set; }

        public override ModuleState GetState()
        {
            throw new System.NotImplementedException();
        }
    }
}