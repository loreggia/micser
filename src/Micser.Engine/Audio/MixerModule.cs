using System;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure;

namespace Micser.Engine.Audio
{
    public class MixerModule : AudioModule
    {
        public IAudioModule Input2 { get; set; }

        public override IModuleState GetState()
        {
            throw new NotImplementedException();
        }
    }
}