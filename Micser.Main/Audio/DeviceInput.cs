using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Micser.Main.Audio
{
    public class DeviceInput : AudioChainLink
    {
        private BufferedWaveProvider _buffer;
        private WasapiCapture _capture;
        private DeviceDescription _deviceDescription;
        private ISampleProvider _sampleProvider;

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
            _buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
            var outputBuffer = new float[e.BytesRecorded / 4];
            int numSamples = outputBuffer.Length;
            while ((numSamples = _sampleProvider.Read(outputBuffer, 0, numSamples)) > 0)
            {
                OnDataAvailable(new AudioInputEventArgs(outputBuffer, numSamples));
            }
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
                _capture.DataAvailable += Capture_DataAvailable;

                _buffer = new BufferedWaveProvider(_capture.WaveFormat);
                _sampleProvider = _buffer.ToSampleProvider();

                _capture.StartRecording();
            }
        }
    }
}