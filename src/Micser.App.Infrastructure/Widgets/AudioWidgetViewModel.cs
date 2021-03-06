﻿using Micser.Common.Modules;

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

        /// <inheritdoc />
        protected AudioWidgetViewModel()
        {
            _volume = 1f;
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the module is muted.
        /// </summary>
        public bool IsMuted
        {
            get => _isMuted;
            set => SetProperty(ref _isMuted, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to use the current system volume.
        /// </summary>
        public bool UseSystemVolume
        {
            get => _useSystemVolume;
            set => SetProperty(ref _useSystemVolume, value);
        }

        /// <summary>
        /// Gets or sets the audio volume.
        /// </summary>
        public float Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        /// <inheritdoc />
        public override ModuleState GetState()
        {
            var state = base.GetState();
            state.IsEnabled = IsEnabled;
            state.Volume = Volume;
            state.IsMuted = IsMuted;
            state.UseSystemVolume = UseSystemVolume;
            return state;
        }

        /// <inheritdoc />
        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            if (!IsInitialized)
            {
                return;
            }

            IsEnabled = state.IsEnabled;
            Volume = state.Volume;
            IsMuted = state.IsMuted;
            UseSystemVolume = state.UseSystemVolume;
        }
    }
}