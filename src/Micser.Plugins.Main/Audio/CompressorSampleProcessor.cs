using CSCore;
using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Extensions;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Audio
{
    public class CompressorSampleProcessor : ISampleProcessor
    {
        private readonly CompressorModule _module;

        private int _attackSamples;

        private bool _isCompressing;
        private int _releaseSamples;

        public CompressorSampleProcessor(CompressorModule module)
        {
            _module = module;
            IsEnabled = true;
            Priority = 50;
        }

        public bool IsEnabled { get; set; }
        public int Priority { get; set; }

        public void Process(WaveFormat waveFormat, ref float value)
        {
            if (_module.Type == CompressorType.Upward)
            {
                ProcessUpward(waveFormat, ref value);
            }
            else
            {
                ProcessDownward(waveFormat, ref value);
            }

            MathExtensions.Clamp(ref value, -1f, 1f);
        }

        public void ProcessDownward(WaveFormat waveFormat, ref float value)
        {
        }

        public void ProcessUpward(WaveFormat waveFormat, ref float value)
        {
            if (_attackSamples > 0)
            {
                _attackSamples--;
                if (_attackSamples == 0)
                {
                    _isCompressing = true;
                }

                return;
            }

            if (_releaseSamples > 0)
            {
                _releaseSamples--;
                return;
            }

            if (value < _module.Threshold)
            {
                var samplesPerMs = waveFormat.SampleRate / 1000f;

                if (_isCompressing)
                {
                    _releaseSamples = (int)(samplesPerMs * _module.Release);
                    return;
                }

                _attackSamples = (int)(samplesPerMs * _module.Attack);
            }
        }
    }
}