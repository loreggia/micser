using System;
using CSCore;
using Micser.Common.Audio;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Audio
{
    public class GainSampleProcessor : ISampleProcessor
    {
        private readonly GainModule _module;

        public GainSampleProcessor(GainModule module)
        {
            _module = module;
            IsEnabled = true;
            Priority = 0;
        }

        public bool IsEnabled { get; set; }
        public int Priority { get; set; }

        public void Process(WaveFormat waveFormat, ref float value)
        {
            var gain = _module.Gain;
            var dbValue = AudioHelper.LinearToDb(Math.Abs(value));
            value = Math.Sign(value) * AudioHelper.DbToLinear(dbValue + gain);
        }
    }
}