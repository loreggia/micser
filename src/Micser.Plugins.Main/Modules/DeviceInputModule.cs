using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;
using Micser.Common.Devices;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Audio;
using NLog;
using System;
using System.Linq;

namespace Micser.Plugins.Main.Modules
{
    public class DeviceInputModule : AudioModule
    {
        private const string DeviceIdKey = "DeviceId";
        private WasapiCapture _capture;
        private DeviceDescription _deviceDescription;
        private SoundInSource _soundInSource;

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

        public override ModuleState GetState()
        {
            return new ModuleState
            {
                Data = { { DeviceIdKey, DeviceDescription?.Id } }
            };
        }

        public override void Initialize(ModuleDto description)
        {
            base.Initialize(description);

            var deviceId = description.ModuleState?.Data.GetObject<string>(DeviceIdKey);
            if (deviceId != null)
            {
                var deviceService = new DeviceService();
                DeviceDescription = deviceService.GetDevices(DeviceType.Input).FirstOrDefault(d => d.Id == deviceId);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                DisposeCapture();
            }
        }

        private void DisposeCapture()
        {
            _soundInSource?.Dispose();
            _soundInSource = null;

            if (_capture != null)
            {
                _capture.Stop();
                _capture.Dispose();
                _capture = null;
            }
        }

        private void InitializeDevice()
        {
            DisposeCapture();

            if (DeviceDescription == null)
            {
                return;
            }

            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                var device = deviceEnumerator.GetDevice(DeviceDescription.Id);
                if (!device.DataFlow.HasFlag(DataFlow.Capture))
                {
                    return;
                }

                _capture = new WasapiCapture(true, AudioClientShareMode.Shared)
                {
                    Device = device
                };

                try
                {
                    _capture.Initialize();
                    _soundInSource = new SoundInSource(_capture) { FillWithZeros = true };
                    Output = _soundInSource;
                    _capture.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, ex);
                }
            }
        }
    }
}