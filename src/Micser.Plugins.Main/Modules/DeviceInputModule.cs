using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using Micser.Engine.Infrastructure.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class DeviceInputModule : DeviceModule
    {
        private WasapiCapture _capture;

        public DeviceInputModule(long id)
            : base(id)
        {
        }

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

        protected override void OnDeviceStateChanged(DeviceState deviceState)
        {
            if (deviceState == DeviceState.Active)
            {
                _capture.Start();
            }
            else
            {
                _capture.Stop();
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
            Write(this, e.Format, e.Data, e.Offset, e.ByteCount);
        }
    }
}