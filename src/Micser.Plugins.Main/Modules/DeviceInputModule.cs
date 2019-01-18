using System;
using System.Linq;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;
using Micser.Common.Devices;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure;
using NLog;

namespace Micser.Plugins.Main.Modules
{
    public class DeviceInputModule : AudioModule
    {
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

        public override IModuleState GetState()
        {
            return new DeviceInputState
            {
                DeviceId = DeviceDescription?.Id
            };
        }

        public override void Initialize(ModuleDescription description)
        {
            base.Initialize(description);

            if (description.State is DeviceInputState state)
            {
                var deviceId = state.DeviceId;
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
                    _soundInSource = new SoundInSource(_capture) {FillWithZeros = true};
                    Output = _soundInSource;
                    _capture.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, ex);
                }
            }
        }

        public class DeviceInputState : IModuleState
        {
            public string DeviceId { get; set; }
        }
    }
}