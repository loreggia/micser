using Micser.App.Infrastructure.Widgets;
using Micser.Plugins.Main.Audio;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Widgets
{
    public class CompressorViewModel : AudioWidgetViewModel
    {
        private float _ratio;
        private float _threshold;
        private CompressorType _type;

        public CompressorViewModel()
        {
            AddInput("Input1");
            AddOutput("Output1");

            Type = CompressorType.Upward;
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
    }
}