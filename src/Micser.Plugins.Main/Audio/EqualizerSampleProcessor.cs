using CSCore;
using CSCore.DSP;
using Micser.Plugins.Main.Modules;
using System.Collections.Generic;
using Micser.Common.Audio;

namespace Micser.Plugins.Main.Audio
{
    public class EqualizerSampleProcessor : SampleProcessor
    {
        protected readonly List<PeakFilter> _filters;
        protected readonly EqualizerModule _module;
        protected int _sampleRate;

        public EqualizerSampleProcessor(EqualizerModule module)
        {
            _filters = new List<PeakFilter>();
            _module = module;
        }

        public override void Process(WaveFormat waveFormat, float[] channelSamples)
        {
            if (_sampleRate != waveFormat.SampleRate ||
                _filters.Count != _module.Filters.Count)
            {
                CreateFilters(waveFormat.SampleRate);
            }
            else
            {
                UpdateFilters();
            }

            foreach (var filter in _filters)
            {
                filter.Process(channelSamples);
            }
        }

        private void CreateFilters(int sampleRate)
        {
            _filters.Clear();

            foreach (var filter in _module.Filters)
            {
                _filters.Add(new PeakFilter(sampleRate, filter.Frequency, filter.BandWidth, filter.PeakGainDb) { Q = filter.Ratio });
            }
        }

        private void UpdateFilters()
        {
            for (var i = 0; i < _module.Filters.Count; i++)
            {
                var filterDescription = _module.Filters[i];
                var filter = _filters[i];
                filter.BandWidth = filterDescription.BandWidth;
                filter.Frequency = filterDescription.Frequency;
                filter.GainDB = filterDescription.PeakGainDb;
                filter.Q = filterDescription.Ratio;
            }
        }
    }
}