using System;
using NAudio.Wave;

namespace Micser.Main.Audio
{
    public class InputToFloatDataConverter
    {
        public WaveFormat WaveFormat { get; set; }

        public float[] ConvertData(byte[] buffer, int count)
        {
            if (WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                switch (WaveFormat.BitsPerSample)
                {
                    case 8:
                        return ConvertPcm8Bit(buffer, count);

                    case 16:
                        return ConvertPcm16Bit(buffer, count);

                    case 24:
                        return ConvertPcm24Bit(buffer, count);

                    case 32:
                        return ConvertPcm32Bit(buffer, count);

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
                        return ConvertSingle(buffer, count);

                    case 64:
                        return ConvertDouble(buffer, count);

                    default:
                        throw new InvalidOperationException(
                            $"Invalid float input format using {WaveFormat.BitsPerSample} bits per sample.");
                }
            }

            throw new InvalidOperationException($"The input uses an unsupported format: {WaveFormat}");
        }

        private static float[] ConvertPcm16Bit(byte[] buffer, int count)
        {
            var result = new float[count / 2];

            for (int i = 0; i < count; i++)
            {
                result[i] = (float)(BitConverter.ToInt16(buffer, i * 2) / 32767d);
            }

            return result;
        }

        private static float[] ConvertPcm24Bit(byte[] buffer, int count)
        {
            var result = new float[count / 3];

            for (int i = 0; i < count; i++)
            {
                var sourceIndex = i * 3;
                result[i] = (float)(((sbyte)buffer[sourceIndex + 2] << 16 | buffer[sourceIndex + 1] << 8 | buffer[sourceIndex]) / 8388608d);
            }

            return result;
        }

        private static float[] ConvertPcm32Bit(byte[] buffer, int count)
        {
            var result = new float[count / 4];

            for (int i = 0; i < count; i++)
            {
                result[i] = (float)(BitConverter.ToInt32(buffer, i * 4) / (double)int.MaxValue);
            }

            return result;
        }

        private static float[] ConvertPcm8Bit(byte[] buffer, int count)
        {
            var result = new float[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = (float)(buffer[i] / 128d - 1.0d);
            }

            return result;
        }

        private float[] ConvertDouble(byte[] buffer, int count)
        {
            throw new NotImplementedException();
        }

        private float[] ConvertSingle(byte[] buffer, int count)
        {
            throw new NotImplementedException();
        }
    }
}