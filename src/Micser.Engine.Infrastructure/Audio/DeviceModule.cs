using CSCore.CoreAudioAPI;
using Micser.Common;
using Micser.Common.Devices;
using Micser.Common.Modules;

namespace Micser.Engine.Infrastructure.Audio
{
    /// <summary>
    /// Base module for audio modules that are associated with a hardware device.
    /// </summary>
    public abstract class DeviceModule : AudioModule
    {
        private DeviceDescription _deviceDescription;

        /// <inheritdoc />
        protected DeviceModule()
        {
            DeviceEnumerator = new MMDeviceEnumerator();
            DeviceEnumerator.DeviceStateChanged += DeviceStateChanged;
        }

        /// <summary>
        /// Gets or sets the currently selected device. Changing the device will call <see cref="InitializeDevice"/>.
        /// </summary>
        public virtual DeviceDescription DeviceDescription
        {
            get => _deviceDescription;
            set
            {
                var oldId = _deviceDescription?.Id;
                _deviceDescription = value;

                if (oldId != _deviceDescription?.Id)
                {
                    InitializeDevice();
                }
            }
        }

        /// <summary>
        /// Gets the CoreAudio device instance.
        /// </summary>
        protected MMDevice Device { get; private set; }

        /// <summary>
        /// Gets the CoreAudio device enumerator.
        /// </summary>
        protected MMDeviceEnumerator DeviceEnumerator { get; }

        /// <inheritdoc />
        public override ModuleState GetState()
        {
            var state = base.GetState();
            state.Data[Globals.StateKeys.DeviceId] = DeviceDescription?.Id;
            return state;
        }

        /// <inheritdoc />
        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            var deviceId = state?.Data.GetObject<string>(Globals.StateKeys.DeviceId);
            if (deviceId != null)
            {
                var deviceService = new DeviceService();
                DeviceDescription = deviceService.GetDescription(deviceId);
            }
            else
            {
                DeviceDescription = null;
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DeviceEnumerator?.Dispose();
                DisposeDevice();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Disposes the <see cref="Device"/>.
        /// </summary>
        protected virtual void DisposeDevice()
        {
            Device?.Dispose();
        }

        /// <summary>
        /// Initializes the <see cref="Device"/>.
        /// </summary>
        protected void InitializeDevice()
        {
            DisposeDevice();

            if (string.IsNullOrEmpty(DeviceDescription?.Id) || (Device = DeviceEnumerator.GetDevice(DeviceDescription.Id)) == null)
            {
                return;
            }

            if (Device.DeviceState == DeviceState.Active)
            {
                OnInitializeDevice();
            }
        }

        /// <summary>
        /// Callback when the state of the active device changes. Calls <see cref="OnInitializeDevice"/> if it is active or <see cref="DisposeDevice"/> otherwise.
        /// </summary>
        /// <param name="deviceState">The new device state.</param>
        protected virtual void OnDeviceStateChanged(DeviceState deviceState)
        {
            if (deviceState == DeviceState.Active)
            {
                OnInitializeDevice();
            }
            else
            {
                DisposeDevice();
            }
        }

        /// <summary>
        /// Callback that is executed after the device has been initialized and is active.
        /// </summary>
        protected virtual void OnInitializeDevice()
        {
        }

        private void DeviceStateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            if (e.DeviceId == DeviceDescription?.Id)
            {
                OnDeviceStateChanged(e.DeviceState);
            }
        }
    }
}