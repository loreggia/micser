using Micser.App.Infrastructure.Widgets;
using Micser.Common.Modules;
using Micser.Plugins.Main.Audio;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Widgets
{
    public class CompressorViewModel : AudioWidgetViewModel
    {
        private float _amount;
        private float _ratio;
        private float _threshold;
        private CompressorType _type;

        public CompressorViewModel()
        {
            AddInput("Input1");
            AddOutput("Output1");

            Amount = 1f;
            Ratio = 2f;
            Threshold = 0f;
            Type = CompressorType.Upward;
        }

        public float Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public override Type ModuleType => typeof(CompressorModule);

        public float Ratio
        {
            get => _ratio;
            set => SetProperty(ref _ratio, value);
        }

        public float Threshold
        {
            get => _threshold;
            set => SetProperty(ref _threshold, value);
        }

        public CompressorType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public override ModuleState GetState()
        {
            var state = base.GetState();
            state.Data[CompressorModule.StateKeys.Amount] = Amount;
            state.Data[CompressorModule.StateKeys.Ratio] = Ratio;
            state.Data[CompressorModule.StateKeys.Threshold] = Threshold;
            state.Data[CompressorModule.StateKeys.Type] = Type;
            return state;
        }

        public override void SetState(ModuleState state)
        {
            base.SetState(state);
            Amount = state.Data.GetObject(CompressorModule.StateKeys.Amount, 1f);
            Ratio = state.Data.GetObject(CompressorModule.StateKeys.Amount, 2f);
            Threshold = state.Data.GetObject(CompressorModule.StateKeys.Amount, 0f);
            Type = state.Data.GetObject(CompressorModule.StateKeys.Amount, CompressorType.Upward);
        }
    }
}