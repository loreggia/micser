using Micser.Common.Modules;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.Audio;
using System;

namespace Micser.Engine.Audio
{
    public class MixerModule : AudioModule
    {
        public IAudioModule Input2 { get; set; }

        public override ModuleState GetState()
        {
            throw new NotImplementedException();
        }
    }
}