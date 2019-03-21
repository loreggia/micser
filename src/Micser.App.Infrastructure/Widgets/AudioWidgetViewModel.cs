using Micser.Common.Modules;

namespace Micser.App.Infrastructure.Widgets
{
    public abstract class AudioWidgetViewModel : WidgetViewModel
    {
        private bool _isMuted;
        private bool _useSystemVolume;
        private float _volume;

        public bool IsMuted
        {
            get => _isMuted;
            set => SetProperty(ref _isMuted, value);
        }

        public bool UseSystemVolume
        {
            get => _useSystemVolume;
            set => SetProperty(ref _useSystemVolume, value);
        }

        public float Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        public override ModuleState GetState()
        {
            var state = base.GetState();
            state.Volume = Volume;
            state.IsMuted = IsMuted;
            state.UseSystemVolume = UseSystemVolume;
            return state;
        }

        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            if (!IsInitialized)
            {
                return;
            }

            Volume = state.Volume;
            IsMuted = state.IsMuted;
            UseSystemVolume = state.UseSystemVolume;
        }
    }
}