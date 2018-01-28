using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Micser.Main.Audio
{
    public class DeviceInput : AudioChainLink
    {
        private WasapiCapture _capture;
        private DeviceDescription _deviceDescription;
        private BufferedWaveProvider _outputBuffer;

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
                _capture.StopRecording();
                _capture.Dispose();
                _capture = null;
            }
        }

        private void Capture_DataAvailable(object sender, WaveInEventArgs e)
        {
            _outputBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        private void InitializeDevice()
        {
            if (DeviceDescription == null)
            {
                Output = null;
                return;
            }

            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                var device = deviceEnumerator.GetDevice(DeviceDescription.Id);
                if (device.DataFlow == DataFlow.Render)
                {
                    Output = null;
                    return;
                }

                _capture = new WasapiCapture(device)
                {
                    ShareMode = AudioClientShareMode.Shared
                };
                _capture.DataAvailable += Capture_DataAvailable;

                _outputBuffer = new BufferedWaveProvider(_capture.WaveFormat);
                Output = _outputBuffer;

                _capture.StartRecording();
            }
        }
    }
}