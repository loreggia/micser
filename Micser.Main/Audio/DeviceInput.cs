using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Micser.Main.Audio
{
    public class DeviceInput : AudioChainLink
    {
        private BufferedWaveProvider _buffer;
        private ISampleProvider _bufferSampleProvider;
        private WasapiCapture _capture;
        private DeviceDescription _deviceDescription;

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

        protected override int ReadInternal(float[] buffer, int offset, int count)
        {
            if (_bufferSampleProvider != null)
            {
                return _bufferSampleProvider.Read(buffer, offset, count);
            }

            return 0;
        }

        private void Capture_DataAvailable(object sender, WaveInEventArgs e)
        {
            _buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
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
                _buffer.DiscardOnBufferOverflow = true;
                _bufferSampleProvider = _buffer.ToSampleProvider();

                _capture.StartRecording();
            }
        }
    }
}