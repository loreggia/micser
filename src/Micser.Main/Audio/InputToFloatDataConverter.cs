using System;
using CSCore;
using Micser.Main.Extensions;

namespace Micser.Main.Audio
{
    public class InputToFloatDataConverter
    {
        private float[] _sharedBuffer;

        public InputToFloatDataConverter()
        {
            // default buffer size of two seconds dual channel 48000Hz audio
            _sharedBuffer = new float[48000 * 2 * 2];
        }

        public float[] ConvertData(byte[] data, int offset, int count, WaveFormat format, out int outputCount)
        {
            outputCount = 0;

            var bitsPerSample = format.BitsPerSample;

            var formatExtensible = format as WaveFormatExtensible;

            if (format.WaveFormatTag == AudioEncoding.Pcm || formatExtensible?.SubFormat == AudioSubTypes.Pcm)
            {
                switch (bitsPerSample)
                {
                    case 8:
                        ConvertPcm8Bit(data, offset, count, out outputCount);
                        break;

                    case 16:
                        ConvertPcm16Bit(data, offset, count, out outputCount);
                        break;

                    case 24:
                        ConvertPcm24Bit(data, offset, count, out outputCount);
                        break;

                    case 32:
                        ConvertPcm32Bit(data, offset, count, out outputCount);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Invalid PCM input format using {format.BitsPerSample} bits per sample.");
                }
            }

            if (format.WaveFormatTag == AudioEncoding.IeeeFloat || formatExtensible?.SubFormat == AudioSubTypes.IeeeFloat)
            {
                switch (bitsPerSample)
                {
                    case 32:
                        ConvertSingle(data, offset, count, out outputCount);
                        break;

                    case 64:
                        ConvertDouble(data, offset, count, out outputCount);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Invalid float input format using {format.BitsPerSample} bits per sample.");
                }
            }

            //Clamp(outputCount);
            return _sharedBuffer;
        }

        private void Clamp(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _sharedBuffer[i] = _sharedBuffer[i].Clamp(-1f, 1f);
            }
        }

        private void ConvertDouble(byte[] data, int offset, int count, out int outputCount)
        {
            outputCount = count / 8;
            EnsureBuffer(outputCount);

            for (int i = offset * 8; i < outputCount; i++)
            {
                _sharedBuffer[i] = (float)BitConverter.ToDouble(data, i * 8);
            }
        }

        private void ConvertPcm16Bit(byte[] data, int offset, int count, out int outputCount)
        {
            outputCount = count / 2;
            EnsureBuffer(outputCount);

            for (int i = offset * 2; i < outputCount; i++)
            {
                _sharedBuffer[i] = (float)(BitConverter.ToInt16(data, i * 2) / 32767d);
            }
        }

        private void ConvertPcm24Bit(byte[] data, int offset, int count, out int outputCount)
        {
            outputCount = count / 3;
            EnsureBuffer(outputCount);

            for (int i = offset * 3; i < outputCount; i++)
            {
                var sourceIndex = i * 3;
                _sharedBuffer[i] = (float)((data[sourceIndex + 2] << 16 | data[sourceIndex + 1] << 8 | data[sourceIndex]) / 8388608d);
            }
        }

        private void ConvertPcm32Bit(byte[] data, int offset, int count, out int outputCount)
        {
            outputCount = count / 4;
            EnsureBuffer(outputCount);

            for (int i = offset * 4; i < outputCount; i++)
            {
                _sharedBuffer[i] = (float)(BitConverter.ToInt32(data, i * 4) / (double)int.MaxValue);
            }
        }

        private void ConvertPcm8Bit(byte[] data, int offset, int count, out int outputCount)
        {
            outputCount = count;
            EnsureBuffer(outputCount);

            for (int i = offset; i < outputCount; i++)
            {
                _sharedBuffer[i] = (float)(data[i] / 128d - 1.0d);
            }
        }

        private void ConvertSingle(byte[] data, int offset, int count, out int outputCount)
        {
            outputCount = count / 4;
            EnsureBuffer(outputCount);

            for (int i = offset * 4; i < outputCount; i++)
            {
                _sharedBuffer[i] = BitConverter.ToSingle(data, i * 4);
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