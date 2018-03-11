using System;
using Micser.Main.Extensions;
using NAudio.Wave;

namespace Micser.Main.Audio
{
    public class InputToFloatDataConverter
    {
        private float[] _sharedBuffer;

        public InputToFloatDataConverter()
        {
            // default buffer size of one second dual channel 44100Hz audio
            _sharedBuffer = new float[88200];
        }

        public WaveFormat WaveFormat { get; set; }

        public float[] ConvertData(byte[] inputBuffer, int inputCount, out int outputCount)
        {
            outputCount = 0;

            if (WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                switch (WaveFormat.BitsPerSample)
                {
                    case 8:
                        ConvertPcm8Bit(inputBuffer, inputCount, out outputCount);
                        break;

                    case 16:
                        ConvertPcm16Bit(inputBuffer, inputCount, out outputCount);
                        break;

                    case 24:
                        ConvertPcm24Bit(inputBuffer, inputCount, out outputCount);
                        break;

                    case 32:
                        ConvertPcm32Bit(inputBuffer, inputCount, out outputCount);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Invalid PCM input format using {WaveFormat.BitsPerSample} bits per sample.");
                }
            }

            if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                switch (WaveFormat.BitsPerSample)
                {
                    case 32:
                        ConvertSingle(inputBuffer, inputCount, out outputCount);
                        break;

                    case 64:
                        ConvertDouble(inputBuffer, inputCount, out outputCount);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Invalid float input format using {WaveFormat.BitsPerSample} bits per sample.");
                }
            }

            Clamp(outputCount);
            return _sharedBuffer;
        }

        private void Clamp(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _sharedBuffer[i] = _sharedBuffer[i].Clamp(-1f, 1f);
            }
        }

        private void ConvertDouble(byte[] buffer, int count, out int outputCount)
        {
            outputCount = count / 8;
            EnsureBuffer(outputCount);

            for (int i = 0; i < outputCount; i++)
            {
                _sharedBuffer[i] = (float)BitConverter.ToDouble(buffer, i * 8);
            }
        }

        private void ConvertPcm16Bit(byte[] buffer, int count, out int outputCount)
        {
            outputCount = count / 2;
            EnsureBuffer(outputCount);

            for (int i = 0; i < outputCount; i++)
            {
                _sharedBuffer[i] = (float)(BitConverter.ToInt16(buffer, i * 2) / 32767d);
            }
        }

        private void ConvertPcm24Bit(byte[] buffer, int count, out int outputCount)
        {
            outputCount = count / 3;
            EnsureBuffer(outputCount);

            for (int i = 0; i < outputCount; i++)
            {
                var sourceIndex = i * 3;
                _sharedBuffer[i] = (float)((buffer[sourceIndex + 2] << 16 | buffer[sourceIndex + 1] << 8 | buffer[sourceIndex]) / 8388608d);
            }
        }

        private void ConvertPcm32Bit(byte[] buffer, int count, out int outputCount)
        {
            outputCount = count / 4;
            EnsureBuffer(outputCount);

            for (int i = 0; i < outputCount; i++)
            {
                _sharedBuffer[i] = (float)(BitConverter.ToInt32(buffer, i * 4) / (double)int.MaxValue);
            }
        }

        private void ConvertPcm8Bit(byte[] buffer, int count, out int outputCount)
        {
            outputCount = count;
            EnsureBuffer(outputCount);

            for (int i = 0; i < outputCount; i++)
            {
                _sharedBuffer[i] = (float)(buffer[i] / 128d - 1.0d);
            }
        }

        private void ConvertSingle(byte[] buffer, int count, out int outputCount)
        {
            outputCount = count / 4;
            EnsureBuffer(outputCount);

            for (int i = 0; i < outputCount; i++)
            {
                _sharedBuffer[i] = BitConverter.ToSingle(buffer, i * 4);
            }
        }

        private void EnsureBuffer(int size)
        {
            if (_sharedBuffer == null || _sharedBuffer.Length < size)
            {
                _sharedBuffer = new float[size];
            }
        }
    }
}