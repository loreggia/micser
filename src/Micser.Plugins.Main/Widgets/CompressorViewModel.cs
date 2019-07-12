using Micser.App.Infrastructure.Widgets;
using Micser.Common.Extensions;
using Micser.Common.Modules;
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

        public bool EnableAdvancedControls
        {
            get => _enableAdvancedControls;
            set
            {
                if (SetProperty(ref _enableAdvancedControls, value) && !value)
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
            set
            {
                if (SetProperty(ref _type, value))
                {
                    CalculateSimpleValues();
                }
            }
        }

        public override ModuleState GetState()
        {
            var state = base.GetState();
            state.Data[nameof(EnableAdvancedControls)] = EnableAdvancedControls;
            return state;
        }

        public override void SetState(ModuleState state)
        {
            // EnableAdvancedControls has to be set before all other values
            EnableAdvancedControls = state.Data.GetObject(nameof(EnableAdvancedControls), false);
            base.SetState(state);
        }

        private void CalculateSimpleValues()
        {
            var amount = Amount;

            if (Type == CompressorType.Upward)
            {
                Attack = 0.05f;
                Knee = 5f;
                Release = 0.01f;
                MakeUpGain = 0f;
                Ratio = MathExtensions.Lerp(1.5f, 5.0f, amount);
                Threshold = MathExtensions.Lerp(-60f, -5f, amount);
            }
            else
            {
                Attack = 0.05f;
                Knee = 5f;
                Release = 0.05f;
                MakeUpGain = MathExtensions.Lerp(0f, 25f, amount);
                Ratio = MathExtensions.Lerp(1.5f, 2.5f, amount);
                Threshold = MathExtensions.Lerp(0f, -40f, amount);
            }
        }
    }
}