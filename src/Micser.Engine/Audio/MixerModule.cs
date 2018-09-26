using Micser.Shared.Models;

namespace Micser.Engine.Audio
{
    public class MixerModule : AudioModule
    {
        public MixerModule()
        {
        }

        public IAudioModule Input2 { get; set; }

        public override string GetState()
        {
            throw new System.NotImplementedException();
        }

        public override void Initialize(AudioModuleDescription description)
        {
            throw new System.NotImplementedException();
        }
    }
}