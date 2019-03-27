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
        private float _attack;
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
            set => SetProperty(ref _amount, value);
        }

        [SaveState(CompressorModule.Defaults.Attack)]
        public float Attack
        {
            get => _attack;
            set => SetProperty(ref _attack, value);
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
    }
}