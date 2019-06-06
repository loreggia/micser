using CSCore;
using Micser.Common.Audio;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Audio
{
    public class GainSampleProcessor : SampleProcessor
    {
        private readonly GainModule _module;

        public GainSampleProcessor(GainModule module)
        {
            _module = module;
        }

        public override void Process(WaveFormat waveFormat, ref float value)
        {
            var gain = _module.Gain;
            var dbValue = AudioHelper.LinearToDb(Math.Abs(value));
            value = Math.Sign(value) * AudioHelper.DbToLinear(dbValue + gain);
        }
    }
}