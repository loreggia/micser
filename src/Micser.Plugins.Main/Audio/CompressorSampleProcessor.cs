using CSCore;
using Micser.Common;
using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Extensions;
using Micser.Plugins.Main.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Plugins.Main.Audio
{
    public class CompressorSampleProcessor : ISampleProcessor
    {
        private const int ChunkSize = 32;
        private readonly IDictionary<int, CircularBuffer<float>> _channelSamples;
        private readonly CompressorModule _module;
        private int _channelIndex;
        private float _rms;

        public CompressorSampleProcessor(CompressorModule module)
        {
            _module = module;
            _channelSamples = new Dictionary<int, CircularBuffer<float>>();

            IsEnabled = true;
            Priority = 50;
        }

        public bool IsEnabled { get; set; }
        public int Priority { get; set; }

        public void Process(WaveFormat waveFormat, ref float value)
        {
            if (!_channelSamples.ContainsKey(_channelIndex))
            {
                _channelSamples[_channelIndex] = new CircularBuffer<float>(ChunkSize);
            }

            _channelSamples[_channelIndex].Add(value);

            CalculateRms(waveFormat.Channels);

            if (_rms >= 0f)
            {
                if (_module.Type == CompressorType.Upward)
                {
                    ProcessUpward(ref value);
                }
                else
                {
                    ProcessDownward(ref value);
                }

                MathExtensions.Clamp(ref value, -1f, 1f);
            }

            _channelIndex = (_channelIndex + 1) % waveFormat.Channels;
        }

        public void ProcessDownward(ref float value)
        {
            var threshold = _module.Threshold;

            if (_rms > threshold)
            {
                var amount = _module.Amount;
                var ratio = _module.Ratio;

                // WA: plot y=x; y=(x+0.5)/2;y=0.5; from x=-1 to 1; from y=-1 to 1
                var amp = _rms - (_rms - (_rms + threshold) / ratio) * amount;
                value *= amp;
            }
        }

        public void ProcessUpward(ref float value)
        {
            var threshold = _module.Threshold;

            if (_rms < threshold)
            {
                var amount = _module.Amount;
                var ratio = _module.Ratio;

                // WA: plot y=x; y=(x+0.5)/2;y=0.5; from x=-1 to 1; from y=-1 to 1
                var y = (_rms + threshold) / ratio;
                var f = y / _rms;
                var fa = (f - 1f) * amount + 1f;
                value *= fa;
            }
        }

        private void CalculateRms(int channelCount)
        {
            if (_channelSamples.Count < channelCount || _channelSamples.Any(b => !b.Value.IsFull))
            {
                _rms = -1f;
                return;
            }

            var sum = 0f;

            for (var ci = 0; ci < channelCount; ci++)
            {
                var buffer = _channelSamples[ci];
                for (var si = 0; si < ChunkSize; si++)
                {
                    var sample = buffer[si];
                    sum += sample * sample;
                }
            }

            _rms = (float)Math.Sqrt(sum / (channelCount * ChunkSize));
        }
    }
}