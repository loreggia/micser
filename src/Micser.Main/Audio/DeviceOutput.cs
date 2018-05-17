using System;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using CSCore.Streams;

namespace Micser.Main.Audio
{
    public class DeviceOutput : AudioChainLink
    {
        private DeviceDescription _deviceDescription;
        private int _latency;
        private WasapiOut _output;
        private IWaveSource _outputBuffer;

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
            if (_output == null || _outputBuffer == null)
            {
                return;
            }

            var inputData = FillChannels(e.Buffer, e.Count, e.ChannelCount, out var count);

            byte[] buffer = null;

            //if (_output.OutputWaveFormat.Encoding == WaveFormatEncoding.Pcm)
            //{
            //    switch (_output.OutputWaveFormat.BitsPerSample)
            //    {
            //        case 8:
            //        case 16:
            //        case 24:
            //        case 32:
            //        case 64:
            //        default:
            //            break;
            //    }
            //}
            //else if (_output.OutputWaveFormat.Encoding == WaveFormatEncoding.IeeeFloat || _output.OutputWaveFormat.Encoding == WaveFormatEncoding.Extensible)
            //{
            //    switch (_output.OutputWaveFormat.BitsPerSample)
            //    {
            //        case 32:
            //            buffer = new byte[count * 4];
            //            Buffer.BlockCopy(inputData, 0, buffer, 0, buffer.Length);
            //            break;

            //        case 64:
            //            buffer = new byte[count * 8];
            //            for (int i = 0; i < count; i++)
            //            {
            //                var bytes = BitConverter.GetBytes(inputData[i]);
            //                Array.Copy(bytes, 0, buffer, i * 8, bytes.Length);
            //            }
            //            break;
            //    }
            //}

            //if (buffer != null)
            //{
            //    _outputBuffer.AddSamples(buffer, 0, buffer.Length);
            //}
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

        private float[] FillChannels(float[] buffer, int count, int inputChannelCount, out int outputCount)
        {
            //var outputChannelCount = _output.OutputWaveFormat.Channels;

            //if (inputChannelCount == outputChannelCount)
            //{
            outputCount = count;
            return buffer;
            //}

            // todo reuse array
            //var result = new float[count / inputChannelCount * outputChannelCount];

            //for (int i = 0; i < count; i++)
            //{
            //    Array.Copy(buffer, i * inputChannelCount, result, i * outputChannelCount, Math.Min(inputChannelCount, outputChannelCount));
            //}

            //outputCount = result.Length;
            //return result;
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

                _output = new WasapiOut(true, AudioClientShareMode.Shared, Latency);
                _outputBuffer = new WriteableBufferingSource(new WaveFormat());
                _output.Initialize(_outputBuffer);
                _output.Play();
            }
        }
    }
}