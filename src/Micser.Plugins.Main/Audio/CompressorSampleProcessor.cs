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

        // not sure what this does exactly, but it is part of the release curve
        private const float SpacingDb = 5f;

        private static readonly float Ang90 = (float)Math.PI * 0.5f;
        private static readonly float Ang90Inv = 2.0f / (float)Math.PI;
        private readonly CompressorModule _module;

        private float _adaptiveReleaseCoeffA;
        private float _adaptiveReleaseCoeffB;
        private float _adaptiveReleaseCoeffC;
        private float _adaptiveReleaseCoeffD;
        private float _attack;
        private float _attackSamplesInv;
        private int _channelCount;
        private float _compGain;
        private float _detectorAvg;
        private float _dry;
        private float _envelopeRate;
        private float _knee;
        private float _linearPreGain;
        private float _masterGain;
        private float _maxCompDiffDb;
        private float _meterGain;
        private float _meterRelease;
        private float _ratio;
        private float _release;
        private int _samplePosition;
        private int _sampleRate;
        private float _satReleaseSamplesInv;
        private float _scaledDesiredGain;
        private float _slope;
        private float _threshold;
        private float _wet;

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
                Math.Abs(_wet - _module.Amount) > AudioModule.Epsilon ||
                Math.Abs(_attack - _module.Attack) > AudioModule.Epsilon ||
                Math.Abs(_release - _module.Release) > AudioModule.Epsilon ||
                Math.Abs(_ratio - _module.Ratio) > AudioModule.Epsilon ||
                Math.Abs(_threshold - _module.Threshold) > AudioModule.Epsilon)
            {
                _sampleRate = waveFormat.SampleRate;
                _channelCount = waveFormat.Channels;
                _samplePosition = 0;

                // todo params
                Initialize(waveFormat.SampleRate, 0f, _module.Threshold, 10f, _module.Ratio, _module.Attack, _module.Release, 0.09f, 0.16f, 0.42f, 0.98f, 0.1f, _module.Amount);
                PrepareChunk();
            }

            ProcessCompressor(ref value);

            _samplePosition++;

            if (_samplePosition >= ChunkSize)
            {
                _samplePosition %= ChunkSize;
                PrepareChunk();
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
            int rate,
            float preGain,
            float threshold,
            float knee,
            float ratio,
            float attack,
            float release,
            float releaseZone1,
            float releaseZone2,
            float releaseZone3,
            float releaseZone4,
            float postGain,
            float wet)
        {
            // useful values
            var linearPreGain = AudioHelper.DbToLinear(preGain);
            var slope = 1.0f / ratio;
            var attackSamples = rate * attack;
            var attackSamplesInv = 1.0f / attackSamples;
            var releaseSamples = rate * release;
            var satRelease = 0.0025f; // seconds
            var satReleaseSamplesInv = 1.0f / (rate * satRelease);
            var dry = 1.0f - wet;

            // metering values (not used in core algorithm, but used to output a meter if desired)
            var meterGain = 1.0f;      // gets overwritten immediately because gain will always be negative
            var meterFallOff = 0.325f; // seconds
            var meterRelease = 1.0f - (float)Math.Exp(-1d / (rate * meterFallOff));

            // calculate a master gain based on what sounds good
            var fullLevel = AudioHelper.CompressorCurve(AudioHelper.LinearToDb(1f), slope, threshold, knee);
            var masterGain = AudioHelper.DbToLinear(postGain) * (float)Math.Pow(1.0f / AudioHelper.DbToLinear(fullLevel), 0.6f);

            // calculate the adaptive release curve parameters
            // solve a,b,c,d in `y = a*x^3 + b*x^2 + c*x + d`
            // intersecting points (0, y1), (1, y2), (2, y3), (3, y4)
            var y1 = releaseSamples * releaseZone1;
            var y2 = releaseSamples * releaseZone2;
            var y3 = releaseSamples * releaseZone3;
            var y4 = releaseSamples * releaseZone4;
            var a = (-y1 + 3.0f * y2 - 3.0f * y3 + y4) / 6.0f;
            var b = y1 - 2.5f * y2 + 2.0f * y3 - 0.5f * y4;
            var c = (-11.0f * y1 + 18.0f * y2 - 9.0f * y3 + 2.0f * y4) / 6.0f;
            var d = y1;

            // save everything
            _attack = attack;
            _release = release;
            _ratio = ratio;
            _meterGain = meterGain;
            _meterRelease = meterRelease;
            _threshold = threshold;
            _knee = knee;
            _wet = wet;
            _linearPreGain = linearPreGain;
            _slope = slope;
            _attackSamplesInv = attackSamplesInv;
            _satReleaseSamplesInv = satReleaseSamplesInv;
            _dry = dry;
            _masterGain = masterGain;
            _adaptiveReleaseCoeffA = a;
            _adaptiveReleaseCoeffB = b;
            _adaptiveReleaseCoeffC = c;
            _adaptiveReleaseCoeffD = d;
            _detectorAvg = 0.0f;
            _compGain = 1.0f;
            _maxCompDiffDb = -1.0f;
        }

        private void PrepareChunk()
        {
            _detectorAvg = Fixf(_detectorAvg, 1.0f);
            _scaledDesiredGain = (float)Math.Asin(_detectorAvg) * Ang90Inv;
            var compDiffDb = AudioHelper.LinearToDb(_compGain / _scaledDesiredGain);

            // calculate envelope rate based on whether we're attacking or releasing
            if (compDiffDb < 0.0f)
            {
                // compgain < scaleddesiredgain, so we're releasing
                compDiffDb = Fixf(compDiffDb, -1.0f);
                _maxCompDiffDb = -1; // reset for a future attack mode
                // apply the adaptive release curve
                // scale compdiffdb between 0-3
                var x = (MathExtensions.Clamp(compDiffDb, -12.0f, 0.0f) + 12.0f) * 0.25f;
                var releasesamples = AudioHelper.AdaptiveReleaseCurve(x, _adaptiveReleaseCoeffA, _adaptiveReleaseCoeffB, _adaptiveReleaseCoeffC, _adaptiveReleaseCoeffD);
                _envelopeRate = AudioHelper.DbToLinear(SpacingDb / releasesamples);
            }
            else
            {
                // compresorgain > scaleddesiredgain, so we're attacking
                compDiffDb = Fixf(compDiffDb, 1.0f);
                if (_maxCompDiffDb == -1 || _maxCompDiffDb < compDiffDb)
                    _maxCompDiffDb = compDiffDb;
                var attenuate = _maxCompDiffDb;
                if (attenuate < 0.5f)
                    attenuate = 0.5f;
                _envelopeRate = 1.0f - (float)Math.Pow(0.25f / attenuate, _attackSamplesInv);
            }
        }

        private void ProcessCompressor(ref float sampleValue)
        {
            //float inputL = input[samplepos].L * linearpregain;
            //float inputR = input[samplepos].R * linearpregain;
            //inputL = Math.Abs(inputL);
            //inputR = Math.Abs(inputR);
            //var inputmax = inputL > inputR ? inputL : inputR;
            var input = sampleValue * _linearPreGain;

            float attenuation;

            if (input < 0.0001f)
            {
                attenuation = 1.0f;
            }
            else
            {
                var inputcompDb = AudioHelper.CompressorCurve(AudioHelper.LinearToDb(input), _slope, _threshold, _knee);
                attenuation = AudioHelper.DbToLinear(inputcompDb) / input;
            }

            float rate;

            if (attenuation > _detectorAvg)
            {
                // if releasing
                var attenuationdb = -AudioHelper.LinearToDb(attenuation);
                if (attenuationdb < 2.0f)
                    attenuationdb = 2.0f;
                var dbpersample = attenuationdb * _satReleaseSamplesInv;
                rate = AudioHelper.DbToLinear(dbpersample) - 1.0f;
            }
            else
            {
                rate = 1.0f;
            }

            _detectorAvg += (attenuation - _detectorAvg) * rate;
            if (_detectorAvg > 1.0f)
            {
                _detectorAvg = 1.0f;
            }
            _detectorAvg = Fixf(_detectorAvg, 1.0f);

            if (_envelopeRate < 1)
            {
                // attack, reduce gain
                _compGain += (_scaledDesiredGain - _compGain) * _envelopeRate;
            }
            else
            {
                // release, increase gain
                _compGain *= _envelopeRate;
                if (_compGain > 1.0f)
                {
                    _compGain = 1.0f;
                }
            }

            // the final gain value!
            var premixGain = (float)Math.Sin(Ang90 * _compGain);
            var gain = _dry + _wet * _masterGain * premixGain;

            // calculate metering (not used in core algo, but used to output a meter if desired)
            var premixGainDb = AudioHelper.LinearToDb(premixGain);
            if (premixGainDb < _meterGain)
            {
                _meterGain = premixGainDb; // spike immediately
            }
            else
            {
                _meterGain += (premixGainDb - _meterGain) * _meterRelease; // fall slowly
            }

            // apply the gain
            sampleValue *= gain;
        }
    }
}