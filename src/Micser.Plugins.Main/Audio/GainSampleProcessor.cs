using CSCore;
using Micser.Common.Audio;
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

        public override void Process(WaveFormat waveFormat, float[] channelSamples)
        {
            var gain = _module.Gain;

            for (int c = 0; c < waveFormat.Channels; c++)
            {
                var dbValue = AudioHelper.LinearToDb(Math.Abs(channelSamples[c]));
                channelSamples[c] = Math.Sign(channelSamples[c]) * AudioHelper.DbToLinear(dbValue + gain);
            }
        }
    }
}