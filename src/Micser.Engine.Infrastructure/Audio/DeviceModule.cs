using CSCore.CoreAudioAPI;
using Micser.Common.Devices;
using Micser.Common.Modules;

namespace Micser.Engine.Infrastructure.Audio
{
    public abstract class DeviceModule : AudioModule
    {
        public const string DeviceIdKey = "DeviceId";

        private DeviceDescription _deviceDescription;

        protected DeviceModule(long id)
            : base(id)
        {
            DeviceEnumerator = new MMDeviceEnumerator();
            DeviceEnumerator.DeviceStateChanged += DeviceStateChanged;
        }

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

        protected MMDevice Device { get; private set; }

        protected MMDeviceEnumerator DeviceEnumerator { get; }

        public override ModuleState GetState()
        {
            var state = base.GetState();
            state.Data[DeviceIdKey] = DeviceDescription?.Id;
            return state;
        }

        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            var deviceId = state?.Data.GetObject<string>(DeviceIdKey);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DeviceEnumerator.Dispose();
                DisposeDevice();
            }

            base.Dispose(disposing);
        }

        protected virtual void DisposeDevice()
        {
            Device?.Dispose();
        }

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