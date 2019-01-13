using Micser.Common.Modules;
using Micser.Engine.Infrastructure;

namespace Micser.Engine.Audio
{
    public class MixerModule : AudioModule
    {
        public MixerModule()
        {
        }

        public IAudioModule Input2 { get; set; }

        public override IModuleState GetState()
        {
            throw new System.NotImplementedException();
        }
    }
}