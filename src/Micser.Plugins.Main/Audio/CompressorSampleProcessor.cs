﻿using CSCore;
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
        private CompressorType _type;

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
                ProcessCompressor(ref value);
            }

            MathExtensions.Clamp(ref value, -1f, 1f);
        }

        private void Initialize(WaveFormat waveFormat)
        {
            _channelCount = waveFormat.Channels;
            _sampleRate = waveFormat.SampleRate;
            _samplePosition = 0;

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

        private void ProcessCompressor(ref float lInput)
        {
            // Level detection - estimate level using peak detector
            var lInputAbs = Math.Abs(lInput);
            var dbInput = lInputAbs < 0.000001 ? -120f : AudioHelper.LinearToDb(lInputAbs);

            // Gain computer - static apply input/output curve
            var dbCompressed = _type == CompressorType.Downward ?
                AudioHelper.CompressorCurveDown(dbInput, _slope, _threshold, _knee) :
                AudioHelper.CompressorCurveUp(dbInput, _slope, _threshold, _knee);

            var dbDiff = dbInput - dbCompressed;
            dbDiff *= _amount;

            if ((_samplePosition %= ChunkSize) == 0)
            {
                _chunkDbDiff = dbDiff;
            }

            // Ballistics - smoothing of the gain
            float dbEnv;
            if (_type == CompressorType.Downward && dbDiff > _envelope ||
                _type == CompressorType.Upward && dbDiff < _envelope)
            {
                dbEnv = _alphaAttack * _envelope + (1 - _alphaAttack) * dbDiff;
            }
            else
            {
                dbEnv = _alphaRelease * _envelope + (1 - _alphaRelease) * dbDiff;
            }
            _envelope = dbEnv;

            // find control
            var dbGain = _makeUpGain * _amount - dbEnv;

            var lGain = AudioHelper.DbToLinear(dbGain);
            lInput *= lGain;

            _samplePosition++;
        }
    }
}