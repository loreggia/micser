﻿using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Micser.Main.Audio
{
    public class DeviceOutput : AudioChainLink
    {
        private DeviceDescription _deviceDescription;
        private int _latency;
        private WasapiOut _output;
        private BufferedWaveProvider _outputBuffer;

        public DeviceOutput()
        {
            Latency = 25;
        }

        /// <summary>
        /// Gets or sets the output device description. The <see cref="DeviceDescription.Id"/>-Property is used to select the device.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the output latency in milliseconds.
        /// </summary>
        public int Latency
        {
            get => _latency;
            set
            {
                if (_latency != value)
                {
                    _latency = value;
                    InitializeDevice();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                DisposeOutput();
            }
        }

        protected override void OnInputDataAvailable(object sender, AudioDataEventArgs e)
        {
            new WaveBuffer()
            _outputBuffer.AddSamples();
        }

        private void DisposeOutput()
        {
            if (_output != null)
            {
                _outputBuffer = null;
                _output.Stop();
                _output.Dispose();
                _output = null;
            }
        }

        private void InitializeDevice()
        {
            DisposeOutput();

            if (string.IsNullOrEmpty(DeviceDescription?.Id))
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

                _output = new WasapiOut(device, AudioClientShareMode.Shared, true, Latency);
                _outputBuffer = new BufferedWaveProvider(_output.OutputWaveFormat);
                _output.Init(_outputBuffer);
                _output.Play();
            }
        }
    }
}