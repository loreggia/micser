﻿using Micser.App.Infrastructure.Widgets;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Extensions;
using Micser.Plugins.Main.Audio;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Widgets
{
    public class CompressorViewModel : AudioWidgetViewModel
    {
        private float _amount;
        private float _attack;
        private bool _enableAdvancedControls;
        private float _knee;
        private float _makeUpGain;
        private float _ratio;
        private float _release;
        private float _threshold;
        private CompressorType _type;

        public CompressorViewModel()
        {
            AddInput("Input1");
            AddOutput("Output1");
        }

        [SaveState(CompressorModule.Defaults.Amount)]
        public float Amount
        {
            get => _amount;
            set
            {
                if (SetProperty(ref _amount, value) && !EnableAdvancedControls)
                {
                    CalculateSimpleValues();
                }
            }
        }

        [SaveState(CompressorModule.Defaults.Attack)]
        public float Attack
        {
            get => _attack;
            set => SetProperty(ref _attack, value);
        }

        [SaveState(false)]
        public bool EnableAdvancedControls
        {
            get => _enableAdvancedControls;
            set
            {
                if (SetProperty(ref _enableAdvancedControls, value) && value)
                {
                    CalculateSimpleValues();
                }
            }
        }

        [SaveState(CompressorModule.Defaults.Knee)]
        public float Knee
        {
            get => _knee;
            set => SetProperty(ref _knee, value);
        }

        [SaveState(CompressorModule.Defaults.MakeUpGain)]
        public float MakeUpGain
        {
            get => _makeUpGain;
            set => SetProperty(ref _makeUpGain, value);
        }

        public override Type ModuleType => typeof(CompressorModule);

        [SaveState(CompressorModule.Defaults.Ratio)]
        public float Ratio
        {
            get => _ratio;
            set => SetProperty(ref _ratio, value);
        }

        [SaveState(CompressorModule.Defaults.Release)]
        public float Release
        {
            get => _release;
            set => SetProperty(ref _release, value);
        }

        [SaveState(CompressorModule.Defaults.Threshold)]
        public float Threshold
        {
            get => _threshold;
            set => SetProperty(ref _threshold, value);
        }

        [SaveState(CompressorModule.Defaults.Type)]
        public CompressorType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private void CalculateSimpleValues()
        {
            var amount = Amount;

            Attack = Type == CompressorType.Upward ? 0.1f : 0.05f;
            Release = 0.1f;
            MakeUpGain = Type == CompressorType.Upward ? 0f : MathExtensions.Lerp(0f, 25f, amount);
            Ratio = MathExtensions.Lerp(1.5f, 2.5f, amount);
            Threshold = Type == CompressorType.Upward ? MathExtensions.Lerp(-60f, -5f, amount) : MathExtensions.Lerp(0f, -40f, amount);
        }
    }
}