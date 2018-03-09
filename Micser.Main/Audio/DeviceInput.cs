using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Micser.Main.Audio
{
    public class DeviceInput : AudioChainLink
    {
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

        public override IAudioChainLink Input
        {
            get => null;
            set => throw new NotImplementedException();
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
                _capture.StartRecording();
            }
        }
    }
}