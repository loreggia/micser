using CSCore.CoreAudioAPI;
using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Devices;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Services;
using System.Threading.Tasks;

namespace Micser.Engine.Infrastructure.Audio
{
    /// <summary>
    /// Base module for audio modules that are associated with a hardware device.
    /// </summary>
    public abstract class DeviceModule : AudioModule
    {
        /// <summary>
        /// The api end point for communication with the UI.
        /// </summary>
        protected readonly IApiEndPoint ApiEndPoint;

        /// <summary>
        /// The module service for access to the module DB.
        /// </summary>
        protected readonly IModuleService ModuleService;

        private DeviceDescription _deviceDescription;

        /// <inheritdoc />
        protected DeviceModule(IApiEndPoint apiEndPoint, IModuleService moduleService)
        {
            ApiEndPoint = apiEndPoint;
            ModuleService = moduleService;

            DeviceEnumerator = new MMDeviceEnumerator();
            DeviceEnumerator.DeviceStateChanged += DeviceStateChanged;
        }

        /// <summary>
        /// Gets or sets the adapter name that is used as a fallback when the device is disconnected or plugged into another USB port.
        /// </summary>
        public string AdapterName { get; set; }

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

                if (value?.AdapterName != null)
                {
                    AdapterName = value.AdapterName;
                }

                if (oldId != value?.Id)
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

        /// <summary>
        /// Gets the type of devices that are available to this module.
        /// </summary>
        protected abstract DeviceType DeviceType { get; }

        /// <inheritdoc />
        public override ModuleState GetState()
        {
            var state = base.GetState();
            state.Data[Globals.StateKeys.DeviceId] = DeviceDescription?.Id;
            state.Data[nameof(AdapterName)] = AdapterName;
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

            if (AdapterName == null)
            {
                AdapterName = state.Data.GetObject<string>(nameof(AdapterName));
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
        /// <param name="deviceDescription">The device description of the device.</param>
        protected virtual void OnDeviceStateChanged(DeviceDescription deviceDescription)
        {
            if (deviceDescription.IsActive)
            {
                if (DeviceDescription == null &&
                    deviceDescription.AdapterName == AdapterName)
                {
                    DeviceDescription = deviceDescription;

                    // save current state and signal UI refresh
                    var moduleDto = ModuleService.GetById(Id);
                    moduleDto.State = GetState();

                    if (ModuleService.Update(moduleDto))
                    {
                        ApiEndPoint.SendMessageAsync(new JsonRequest("modules", "refresh", moduleDto));
                    }
                }
                else
                {
                    OnInitializeDevice();
                }
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

        private async void DeviceStateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            var deviceService = new DeviceService();
            var deviceDescription = deviceService.GetDescription(e.DeviceId);

            if (e.DeviceId == DeviceDescription?.Id ||
                deviceDescription.Type == DeviceType &&
                deviceDescription.AdapterName == AdapterName)
            {
                await Task.Run(() => OnDeviceStateChanged(deviceDescription)).ConfigureAwait(false);
            }
        }
    }
}