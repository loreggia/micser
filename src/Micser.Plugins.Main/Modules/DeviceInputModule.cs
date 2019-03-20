using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using Micser.Common.Devices;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Audio;
using System.Linq;

namespace Micser.Plugins.Main.Modules
{
    public class DeviceInputModule : AudioModule
    {
        private const string DeviceIdKey = "DeviceId";
        private WasapiCapture _capture;
        private DeviceDescription _deviceDescription;

        public DeviceInputModule(long id)
            : base(id)
        {
        }

        public DeviceDescription DeviceDescription
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

        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            var deviceId = state?.Data.GetObject<string>(DeviceIdKey);
            if (deviceId != null)
            {
                var deviceService = new DeviceService();
                DeviceDescription = deviceService.GetDevices(DeviceType.Input).FirstOrDefault(d => d.Id == deviceId);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _capture.Stop();
                _capture.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeDevice()
        {
            if (string.IsNullOrEmpty(DeviceDescription?.Id) || _capture != null && _capture.Device.DeviceID != DeviceDescription.Id)
            {
                _capture?.Stop();
                _capture?.Dispose();
                _capture = null;
            }

            if (DeviceDescription == null)
            {
                return;
            }

            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                var device = deviceEnumerator.GetDevice(DeviceDescription.Id);

                if (device == null)
                {
                    return;
                }

                _capture = new WasapiCapture(true, AudioClientShareMode.Shared) { Device = device };

                _capture.Initialize();
                _capture.DataAvailable += (s, e) =>
                {
                    Write(this, e.Format, e.Data, e.Offset, e.ByteCount);
                };
                _capture.Start();
            }
        }
    }
}