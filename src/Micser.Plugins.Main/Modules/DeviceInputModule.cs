using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using Microsoft.Extensions.Logging;
using Micser.Common.Devices;
using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Services;

namespace Micser.Plugins.Main.Modules
{
    public class DeviceInputModule : DeviceModule
    {
        private WasapiCapture _capture;

        public DeviceInputModule(IModuleService moduleService, ILogger<DeviceInputModule> logger)
            : base(moduleService, logger)
        {
        }

        protected override DeviceType DeviceType => DeviceType.Input;

        protected virtual WasapiCapture CreateCapture()
        {
            return new WasapiCapture(true, AudioClientShareMode.Shared) { Device = Device };
        }

        protected override void DisposeDevice()
        {
            if (_capture != null)
            {
                _capture.Stop();
                _capture.DataAvailable -= OnCaptureDataAvailable;
                _capture.Dispose();
                _capture = null;
            }

            base.DisposeDevice();
        }

        protected override void OnDeviceStateChanged(DeviceDescription deviceDescription)
        {
            base.OnDeviceStateChanged(deviceDescription);

            if (deviceDescription.IsActive)
            {
                _capture?.Start();
            }
            else
            {
                _capture?.Stop();
            }
        }

        protected override void OnInitializeDevice()
        {
            base.OnInitializeDevice();

            _capture = CreateCapture();
            _capture.DataAvailable += OnCaptureDataAvailable;
            _capture.Initialize();
            _capture.Start();
        }

        private void OnCaptureDataAvailable(object sender, DataAvailableEventArgs e)
        {
            if (IsEnabled)
            {
                Write(this, e.Format, e.Data, e.Offset, e.ByteCount);
            }
        }
    }
}