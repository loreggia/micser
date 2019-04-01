using CSCore;
using Micser.Common.Audio;
using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Extensions;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Audio
{
    // https://code.soundsoftware.ac.uk/projects/audio_effects_textbook_code/repository
    public class CompressorSampleProcessor : ISampleProcessor
    {
        private const int ChunkSize = 32;

        private readonly CompressorModule _module;
        private float _alphaAttack;
        private float _alphaRelease;
        private float _amount;
        private float _attack;
        private int _channelCount;
        private float _chunkDbDiff;
        private float _envelope;
        private float _knee;
        private float _makeUpGain;
        private float _ratio;
        private float _release;
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
                Math.Abs(_makeUpGain - _module.MakeUpGain) > AudioModule.Epsilon ||
                Math.Abs(_release - _module.Release) > AudioModule.Epsilon ||
                Math.Abs(_ratio - _module.Ratio) > AudioModule.Epsilon ||
                Math.Abs(_threshold - _module.Threshold) > AudioModule.Epsilon)
            {
                _sampleRate = waveFormat.SampleRate;
                _channelCount = waveFormat.Channels;
                _samplePosition = 0;

                Initialize(waveFormat.SampleRate, _module.Threshold, _module.Knee, _module.Ratio, _module.Attack, _module.Release, _module.Amount, _module.MakeUpGain);
                //PrepareChunk();
            }

            ProcessCompressor(ref value);

            MathExtensions.Clamp(ref value, -1f, 1f);
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
            float amount,
            float makeUpGain)
        {
            // useful values
            var slope = 1.0f / ratio;
            var attackSamples = sampleRate * attack;
            var releaseSamples = sampleRate * release;
            _alphaAttack = (float)Math.Exp(-1f / attackSamples);
            _alphaRelease = (float)Math.Exp(-1f / releaseSamples);

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
            _envelope = 0f;
            _makeUpGain = makeUpGain;
        }

        private void ProcessCompressor(ref float lInput)
        {
            // Level detection - estimate level using peak detector
            var lInputAbs = Math.Abs(lInput);
            var dbInput = lInputAbs < 0.000001 ? -120f : AudioHelper.LinearToDb(lInputAbs);

            // Gain computer - static apply input/output curve
            var dbCompressed = AudioHelper.CompressorCurve(dbInput, _slope, _threshold, _knee);

            var dbDiff = dbInput - dbCompressed;

            if ((_samplePosition %= ChunkSize) == 0)
            {
                _chunkDbDiff = dbDiff;
            }

            // Ballistics - smoothing of the gain
            float dbEnv;
            if (_chunkDbDiff > _envelope)
            {
                dbEnv = _alphaAttack * _envelope + (1 - _alphaAttack) * dbDiff;
            }
            else
            {
                dbEnv = _alphaRelease * _envelope + (1 - _alphaRelease) * dbDiff;
            }
            _envelope = dbEnv;

            // find control
            var lGain = AudioHelper.DbToLinear(_makeUpGain - dbEnv);
            lInput *= lGain;

            _samplePosition++;
        }
    }
}