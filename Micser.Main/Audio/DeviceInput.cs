using NAudio.CoreAudioApi;
using NAudio.Wave;

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

        private void Capture_DataAvailable(object sender, WaveInEventArgs e)
        {
            var floatBuffer = _inputConverter.ConvertData(e.Buffer, e.BytesRecorded, out var count);
            OnDataAvailable(floatBuffer, count, _capture.WaveFormat.Channels);
        }

        private void DisposeCapture()
        {
            if (_capture != null)
            {
                _capture.DataAvailable -= Capture_DataAvailable;
                _capture.StopRecording();
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

                _capture = new WasapiCapture(device)
                {
                    ShareMode = AudioClientShareMode.Shared
                };
                _capture.StartRecording();
                var defaultFormat = _capture.WaveFormat;
                var newFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, defaultFormat.Channels);
                _capture.WaveFormat = newFormat;
                _inputConverter.WaveFormat = newFormat;
                _capture.DataAvailable += Capture_DataAvailable;
            }
        }
    }
}