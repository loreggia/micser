using Micser.Common;
using Micser.Engine.Infrastructure;
using System;

namespace Micser.Engine.Audio
{
    public class MixerModule : AudioModule
    {
        public IAudioModule Input2 { get; set; }

        public override StateDictionary GetState()
        {
            throw new NotImplementedException();
        }
    }
}