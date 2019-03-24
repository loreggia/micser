using CSCore.CoreAudioAPI;
using Micser.Common.Devices;
using Micser.Common.Modules;

namespace Micser.Engine.Infrastructure.Audio
{
    public abstract class DeviceModule : AudioModule
    {
        public const string DeviceIdKey = "DeviceId";

        private readonly AudioEndpointVolumeCallback _endpointVolumeCallback;
        private DeviceDescription _deviceDescription;
        private AudioEndpointVolume _endpointVolume;

        protected DeviceModule(long id)
                            : base(id)
        {
            DeviceEnumerator = new MMDeviceEnumerator();
            DeviceEnumerator.DeviceStateChanged += DeviceStateChanged;
            _endpointVolumeCallback = new AudioEndpointVolumeCallback();
            _endpointVolumeCallback.NotifyRecived += VolumeNotifyReceived;
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
                _endpointVolumeCallback.NotifyRecived -= VolumeNotifyReceived;
                DeviceEnumerator.Dispose();
                DisposeDevice();
            }

            base.Dispose(disposing);
        }

        protected virtual void DisposeDevice()
        {
            _endpointVolume?.UnregisterControlChangeNotify(_endpointVolumeCallback);
            _endpointVolume?.Dispose();
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
            _endpointVolume = AudioEndpointVolume.FromDevice(Device);
            _endpointVolume.RegisterControlChangeNotify(_endpointVolumeCallback);
        }

        protected virtual void OnVolumeChanged(float volume, bool isMuted, int channelCount, float[] channelVolumes)
        {
        }

        private void DeviceStateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            if (e.DeviceId == DeviceDescription?.Id)
            {
                OnDeviceStateChanged(e.DeviceState);
            }
        }

        private void VolumeNotifyReceived(object sender, AudioEndpointVolumeCallbackEventArgs e)
        {
            OnVolumeChanged(e.MasterVolume, e.IsMuted, e.Channels, e.ChannelVolumes);
        }
    }
}