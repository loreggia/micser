﻿using CSCore;
using Micser.Common.Audio;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Audio
{
    // https://code.soundsoftware.ac.uk/projects/audio_effects_textbook_code/repository
    public class CompressorSampleProcessor : SampleProcessor
    {
        private const int ChannelChunkSize = 32;

        private readonly CompressorModule _module;
        private float _alphaAttack;
        private float _alphaRelease;
        private float _amount;
        private float _attack;
        private int _channelCount;
        private float _chunkMaxDbDiff;
        private int _chunkSize;
        private float _envelope;
        private float _knee;
        private float _makeUpGain;
        private float _ratio;
        private float _release;
        private int _samplePosition;
        private int _sampleRate;
        private float _slope;
        private float _threshold;
        private CompressorType _type;

        public CompressorSampleProcessor(CompressorModule module)
        {
            _module = module;
            Priority = 50;
        }

        public override void Process(WaveFormat waveFormat, float[] channelSamples)
        {
            var typeChanged = _type != _module.Type;
            if (waveFormat.SampleRate != _sampleRate ||
                waveFormat.Channels != _channelCount ||
                Math.Abs(_amount - _module.Amount) > AudioModule.Epsilon ||
                Math.Abs(_attack - _module.Attack) > AudioModule.Epsilon ||
                Math.Abs(_makeUpGain - _module.MakeUpGain) > AudioModule.Epsilon ||
                Math.Abs(_release - _module.Release) > AudioModule.Epsilon ||
                Math.Abs(_ratio - _module.Ratio) > AudioModule.Epsilon ||
                Math.Abs(_threshold - _module.Threshold) > AudioModule.Epsilon ||
                typeChanged)
            {
                Initialize(waveFormat);

                if (typeChanged)
                {
                    _envelope = 0f;
                }
            }

            if (Math.Abs(_slope) < 1f)
            {
                var average = 0f;
                for (int c = 0; c < _channelCount; c++)
                {
                    average += channelSamples[c];
                }
                average /= _channelCount;
                var lGain = ProcessCompressor(average);

                for (int c = 0; c < _channelCount; c++)
                {
                    channelSamples[c] *= lGain;
                }
            }
        }

        private void Initialize(WaveFormat waveFormat)
        {
            _channelCount = waveFormat.Channels;
            _sampleRate = waveFormat.SampleRate;
            _samplePosition = 0;

            _chunkSize = _channelCount * ChannelChunkSize;

            _slope = 1.0f / _module.Ratio;

            var attackSamples = _sampleRate * _module.Attack;
            _alphaAttack = (float)Math.Exp(-1f / attackSamples);

            var releaseSamples = _sampleRate * _module.Release;
            _alphaRelease = (float)Math.Exp(-1f / releaseSamples);

            _type = _module.Type;
            _attack = _module.Attack;
            _release = _module.Release;
            _ratio = _module.Ratio;
            _threshold = _module.Threshold;
            _knee = _module.Knee;
            _amount = _module.Amount;
            _makeUpGain = _module.MakeUpGain;
        }

        private float ProcessCompressor(float lInput)
        {
            var lInputAbs = Math.Abs(lInput);

            // don't process zero values, otherwise a loud crack is heard when audio starts after being silent
            if (lInputAbs < float.Epsilon)
            {
                return 1f;
            }

            // Level detection - estimate level using peak detector
            var dbInput = lInputAbs < 0.000001 ? -120f : AudioHelper.LinearToDb(lInputAbs);

            // Gain computer - static apply input/output curve
            var dbCompressed = _type == CompressorType.Downward ?
                AudioHelper.CompressorCurveDown(dbInput, _slope, _threshold, _knee) :
                AudioHelper.CompressorCurveUp(dbInput, _slope, _threshold, _knee);

            var dbDiff = dbInput - dbCompressed;
            dbDiff *= _amount;

            if ((_samplePosition %= _chunkSize) == 0)
            {
                _chunkMaxDbDiff = dbDiff;
            }
            else
            {
                _chunkMaxDbDiff = Math.Max(_chunkMaxDbDiff, dbDiff);
            }

            // Ballistics - smoothing of the gain
            float dbEnv;
            if (_type == CompressorType.Downward && _chunkMaxDbDiff > _envelope ||
                _type == CompressorType.Upward && _chunkMaxDbDiff < _envelope)
            {
                dbEnv = _alphaAttack * _envelope + (1 - _alphaAttack) * _chunkMaxDbDiff;
            }
            else
            {
                dbEnv = _alphaRelease * _envelope + (1 - _alphaRelease) * _chunkMaxDbDiff;
            }
            _envelope = dbEnv;

            _samplePosition++;

            var dbGain = _makeUpGain * _amount - dbEnv;
            return AudioHelper.DbToLinear(dbGain);
        }
    }
}