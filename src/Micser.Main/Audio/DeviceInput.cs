using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;

namespace Micser.Main.Audio
{
    public class DeviceInput : AudioChainLink
    {
        private WasapiCapture _capture;
        private WriteableBufferingSource _captureBuffer;
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

        private void Capture_DataAvailable(object sender, DataAvailableEventArgs e)
        {
            _captureBuffer.Write(e.Data, e.Offset, e.ByteCount);
        }

        private void DisposeCapture()
        {
            _captureBuffer?.Dispose();
            _captureBuffer = null;

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
                _captureBuffer = new WriteableBufferingSource(_capture.WaveFormat) { FillWithZeros = true };
                _capture.Start();
            }
        }
    }
}