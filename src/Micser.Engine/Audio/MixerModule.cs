using Micser.Engine.Infrastructure;
using Micser.Infrastructure.Models;

namespace Micser.Engine.Audio
{
    public class MixerModule : Module
    {
        public MixerModule()
        {
        }

        public IModule Input2 { get; set; }

        public override ModuleState GetState()
        {
            throw new System.NotImplementedException();
        }
    }
}