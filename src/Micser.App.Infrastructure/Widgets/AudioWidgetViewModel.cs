using Micser.Common.Modules;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Base class for widgets that handle audio streams. Add an <see cref="AudioWidgetControls"/> element to the view to handle this view model's properties automatically.
    /// </summary>
    public abstract class AudioWidgetViewModel : WidgetViewModel
    {
        private bool _isMuted;

        private bool _useSystemVolume;

        private float _volume;

        protected AudioWidgetViewModel()
        {
            Volume = 1f;
        }

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