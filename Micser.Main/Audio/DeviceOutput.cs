using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Micser.Main.Audio
{
    public class DeviceOutput : AudioChainLink
    {
        private DeviceDescription _deviceDescription;
        private WasapiOut _output;
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
                _output?.Stop();
                _output?.Dispose();
                _output = null;
                _outputBuffer = null;
            }
        }

        protected override void OnInputDataAvailable(object sender, AudioInputEventArgs e)
        {
            _outputBuffer?.AddSamples(e.Buffer, 0, e.Count);
        }

        private void InitializeDevice()
        {
            if (DeviceDescription == null)
            {
                return;
            }

            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                var device = deviceEnumerator.GetDevice(DeviceDescription.Id);
                if (!device.DataFlow.HasFlag(DataFlow.Render))
                {
                    return;
                }

                // todo
                _output = new WasapiOut(device, AudioClientShareMode.Shared, true, 50);
                _outputBuffer = new BufferedWaveProvider(_output.OutputWaveFormat);
                _output.Init(_outputBuffer);
                _output.Play();
            }
        }
    }
}