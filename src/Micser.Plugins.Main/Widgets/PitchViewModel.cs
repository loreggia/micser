using Micser.App.Infrastructure.Widgets;
using Micser.Common.Modules;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Widgets
{
    public class PitchViewModel : AudioWidgetViewModel
    {
        private int _fftSize;
        private float _pitch;

        private int _quality;

        public PitchViewModel()
        {
            AddInput("Input1");
            AddOutput("Output1");
        }

        [SaveState(PitchModule.Defaults.FftSize)]
        public int FftSize
        {
            get => _fftSize;
            set => SetProperty(ref _fftSize, value);
        }

        public override Type ModuleType => typeof(PitchModule);

        [SaveState(PitchModule.Defaults.Pitch)]
        public float Pitch
        {
            get => _pitch;
            set => SetProperty(ref _pitch, value);
        }

        [SaveState(PitchModule.Defaults.Quality)]
        public int Quality
        {
            get => _quality;
            set => SetProperty(ref _quality, value);
        }
    }
}