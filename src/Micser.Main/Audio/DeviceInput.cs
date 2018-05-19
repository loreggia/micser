using CSCore.CoreAudioAPI;
using CSCore.SoundIn;

namespace Micser.Main.Audio
{
    public class DeviceInput : AudioChainLink
    {
        private readonly InputToFloatDataConverter _inputConverter;
        private WasapiCapture _capture;
        private DeviceDescription _deviceDescription;

        public DeviceInput()
        {
            _inputConverter = new InputToFloatDataConverter();
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                DisposeCapture();
            }
        }

        private void Capture_DataAvailable(object sender, DataAvailableEventArgs e)
        {
            var floatBuffer = _inputConverter.ConvertData(e.Data, e.Offset, e.ByteCount, e.Format, out var count);
            OnDataAvailable(floatBuffer, count, _capture.WaveFormat.Channels);
        }

        private void DisposeCapture()
        {
            if (_capture != null)
            {
                _capture.DataAvailable -= Capture_DataAvailable;
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

                _capture = new WasapiCapture(true, AudioClientShareMode.Shared);
                _capture.DataAvailable += Capture_DataAvailable;
                _capture.Initialize();
                _inputConverter.WaveFormat = _capture.WaveFormat;
                _capture.Start();
            }
        }
    }
}