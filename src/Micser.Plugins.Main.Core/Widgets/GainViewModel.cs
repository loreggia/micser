using Micser.App.Infrastructure.Widgets;
using Micser.Common.Modules;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Widgets
{
    public class GainViewModel : AudioWidgetViewModel
    {
        private float _gain;

        public GainViewModel()
        {
            AddInput("Input1");
            AddOutput("Output1");
            Volume = 1f;
        }

        [SaveState(0f)]
        public float Gain
        {
            get => _gain;
            set => SetProperty(ref _gain, value);
        }

        public override Type ModuleType => typeof(GainModule);
    }
}