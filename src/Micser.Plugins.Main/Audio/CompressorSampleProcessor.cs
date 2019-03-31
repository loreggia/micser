using CSCore;
using Micser.Common.Audio;
using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Extensions;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Audio
{
    // Most of this code is based on the sndfilter project from Sean Connelly (@voidqk), http://syntheti.cc
    // Project Home: https://github.com/voidqk/sndfilter
    public class CompressorSampleProcessor : ISampleProcessor
    {
        private const int ChunkSize = 32;

        private readonly CompressorModule _module;
        private float _amount;
        private float _attack;
        private float _attackSamples;
        private int _channelCount;
        private float _knee;
        private float _ratio;
        private float _release;
        private float _releaseSamples;
        private int _samplePosition;
        private int _sampleRate;
        private float _slope;
        private float _threshold;

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
            if (waveFormat.SampleRate != _sampleRate ||
                waveFormat.Channels != _channelCount ||
                Math.Abs(_amount - _module.Amount) > AudioModule.Epsilon ||
                Math.Abs(_attack - _module.Attack) > AudioModule.Epsilon ||
                Math.Abs(_release - _module.Release) > AudioModule.Epsilon ||
                Math.Abs(_ratio - _module.Ratio) > AudioModule.Epsilon ||
                Math.Abs(_threshold - _module.Threshold) > AudioModule.Epsilon)
            {
                _sampleRate = waveFormat.SampleRate;
                _channelCount = waveFormat.Channels;
                _samplePosition = 0;

                Initialize(waveFormat.SampleRate, _module.Threshold, _module.Knee, _module.Ratio, _module.Attack, _module.Release, _module.Amount);
                //PrepareChunk();
            }

            ProcessCompressor(ref value);

            _samplePosition++;

            if (_samplePosition >= ChunkSize)
            {
                _samplePosition %= ChunkSize;
                //PrepareChunk();
            }

            MathExtensions.Clamp(ref value, -1f, 1f);
        }

        private static float Fixf(float v, float def)
        {
            // fix NaN and infinity values that sneak in... not sure why this is needed, but it is
            if (float.IsNaN(v) || float.IsInfinity(v))
                return def;
            return v;
        }

        /// <summary>
        /// this is the main initialization function
        /// it does a bunch of pre-calculation so that the inner loop of signal processing is fast
        /// </summary>
        private void Initialize(
            int sampleRate,
            float threshold,
            float knee,
            float ratio,
            float attack,
            float release,
            float amount)
        {
            // useful values
            var slope = 1.0f / ratio;
            _attackSamples = sampleRate * attack;
            _releaseSamples = sampleRate * release;

            // calculate a master gain based on what sounds good
            //var fullLevel = AudioHelper.CompressorCurve(AudioHelper.LinearToDb(1f), slope, threshold, knee);
            //var masterGain = AudioHelper.DbToLinear(postGain) * (float)Math.Pow(1.0f / AudioHelper.DbToLinear(fullLevel), 0.6f);

            // save everything
            _attack = attack;
            _release = release;
            _ratio = ratio;
            _threshold = threshold;
            _knee = knee;
            _amount = amount;
            _slope = slope;
        }

        private void ProcessCompressor(ref float linearInput)
        {
            //var dbInput = AudioHelper.LinearToDb(linearInput);

            //var compDb = AudioHelper.CompressorCurve(dbValue, _slope, _threshold, _knee);
            //var attenuation = AudioHelper.DbToLinear(compDb) / sampleValue;
            var x = linearInput;
            var T = _threshold;
            var tauAttack = _attack * 1000;
            var tauRelease = _release * 1000;
            var W = _knee;
            var M = 0f;
            var fs = _sampleRate / 1000;

            // level detection
            var y_L = Math.Max(Math.Abs(x), float.Epsilon);

            // var decibel conversion
            var x_dB = y_L;
            var y_dB = AudioHelper.LinearToDb(x_dB);
            var x_G = y_dB;

            // gain computer
            var slope = 1f / _ratio - 1f;
            var overshoot = x_G - _threshold;
            var y_G = 0f;
            if (overshoot <= -W / 2f)
            {
                y_G = x_G;
            }
            else if (overshoot > -W / 2f && overshoot < W / 2f)
            {
                y_G = x_G + slope * (overshoot + W / 2f) * (overshoot + W / 2f) / 2f * W;
            }
            else if (overshoot >= -W / 2f)
            {
                y_G = x_G + slope * overshoot;
            }

            var x_T = y_G - x_G;

            // ballistics
            var alphaAtt = (float)Math.Exp(-1f / (tauAttack * fs));
            var alphaRel = (float)Math.Exp(-1f / (tauRelease * fs));

            var y_T = 0f;
            if (x_T > 0)
            {
                y_T = (1 - alphaAtt) * x_T;
            }
            else
            {
                y_T = (1 - alphaRel) * x_T;
            }
        }
    }
}