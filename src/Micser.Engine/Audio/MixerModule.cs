using Micser.Infrastructure.Models;
using Micser.Infrastructure.Modules;

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