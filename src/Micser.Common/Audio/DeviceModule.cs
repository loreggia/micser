using System.Threading.Tasks;
using CSCore.CoreAudioAPI;
using Microsoft.Extensions.Logging;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using Micser.Common.Services;

namespace Micser.Common.Audio
{
    /// <summary>
    /// Base module for audio modules that are associated with a hardware device.
    /// </summary>
    public abstract class DeviceModule : AudioModule
    {
        /// <summary>
        /// The module service for access to the module DB.
        /// </summary>
        protected readonly IModuleService ModuleService;

        private DeviceDescription? _deviceDescription;

        /// <inheritdoc />
        protected DeviceModule(IModuleService moduleService, ILogger logger)
            : base(logger)
        {
            ModuleService = moduleService;

            DeviceEnumerator = new MMDeviceEnumerator();
            DeviceEnumerator.DeviceStateChanged += DeviceStateChanged;
        }

        /// <summary>
        /// Gets or sets the adapter name that is used as a fallback when the device is disconnected or plugged into another USB port.
        /// </summary>
        public string? AdapterName { get; set; }

        /// <summary>
        /// Gets or sets the currently selected device. Changing the device will call <see cref="InitializeDevice"/>.
        /// </summary>
        public virtual DeviceDescription? DeviceDescription
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
        protected MMDevice? Device { get; private set; }

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
            state[Globals.StateKeys.DeviceId] = DeviceDescription?.Id;
            state[nameof(AdapterName)] = AdapterName;
            return state;
        }

        /// <inheritdoc />
        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            var deviceId = state.GetObject<string>(Globals.StateKeys.DeviceId);
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
                AdapterName = state.GetObject<string>(nameof(AdapterName));
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DeviceEnumerator.Dispose();
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
        protected virtual async void OnDeviceStateChanged(DeviceDescription deviceDescription)
        {
            if (deviceDescription.IsActive)
            {
                if (DeviceDescription == null &&
                    deviceDescription.AdapterName == AdapterName)
                {
                    DeviceDescription = deviceDescription;

                    // save current state and signal UI refresh
                    var moduleDto = await ModuleService.GetByIdAsync(Id).ConfigureAwait(false);

                    if (moduleDto != null)
                    {
                        moduleDto.State.AddRange(GetState());
                        await ModuleService.UpdateAsync(moduleDto).ConfigureAwait(false);
                        // TODO
                        //ApiClient.SendMessageAsync("modules", "refresh", moduleDto);
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

        private async void DeviceStateChanged(object? sender, DeviceStateChangedEventArgs e)
        {
            var deviceService = new DeviceService();
            var deviceDescription = deviceService.GetDescription(e.DeviceId);

            if (e.DeviceId == DeviceDescription?.Id ||
                (deviceDescription != null && deviceDescription.Type == DeviceType && deviceDescription.AdapterName == AdapterName))
            {
                await Task.Run(() => OnDeviceStateChanged(deviceDescription!)).ConfigureAwait(false);
            }
        }
    }
}