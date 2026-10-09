using System.Buffers.Binary;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace Micser.Audio.Devices;

/// <summary>
/// Converts interleaved device samples (IEEE float or 16/24/32 bit PCM) to interleaved floats.
/// </summary>
internal sealed class SampleConverter
{
    private static readonly Guid IeeeFloatSubFormat = new("00000003-0000-0010-8000-00aa00389b71");
    private static readonly Guid PcmSubFormat = new("00000001-0000-0010-8000-00aa00389b71");

    private readonly int _bytesPerSample;
    private readonly bool _isFloat;

    public SampleConverter(WaveFormat format)
    {
        _isFloat =
            format.Encoding == WaveFormatEncoding.IeeeFloat
            || (format is WaveFormatExtensible { SubFormat: var floatFormat } && floatFormat == IeeeFloatSubFormat);
        var isPcm =
            format.Encoding == WaveFormatEncoding.Pcm
            || (format is WaveFormatExtensible { SubFormat: var pcmFormat } && pcmFormat == PcmSubFormat);
        _bytesPerSample = format.BitsPerSample / 8;

        if (!(_isFloat && _bytesPerSample == 4) && !(isPcm && _bytesPerSample is 2 or 3 or 4))
        {
            throw new NotSupportedException($"Unsupported sample format: {format}.");
        }
    }

    public int BytesPerSample => _bytesPerSample;

    /// <summary>
    /// Converts <paramref name="source"/> into <paramref name="destination"/> and returns the number of samples written.
    /// </summary>
    public int Convert(ReadOnlySpan<byte> source, Span<float> destination)
    {
        var count = Math.Min(source.Length / _bytesPerSample, destination.Length);

        if (_isFloat)
        {
            MemoryMarshal.Cast<byte, float>(source[..(count * 4)]).CopyTo(destination);
            return count;
        }

        switch (_bytesPerSample)
        {
            case 2:
                for (var i = 0; i < count; i++)
                {
                    destination[i] = BinaryPrimitives.ReadInt16LittleEndian(source[(i * 2)..]) / 32768f;
                }

                break;

            case 3:
                for (var i = 0; i < count; i++)
                {
                    var offset = i * 3;
                    var value = (source[offset] << 8) | (source[offset + 1] << 16) | (source[offset + 2] << 24);
                    destination[i] = value / 2147483648f;
                }

                break;

            default:
                for (var i = 0; i < count; i++)
                {
                    destination[i] = BinaryPrimitives.ReadInt32LittleEndian(source[(i * 4)..]) / 2147483648f;
                }

                break;
        }

        return count;
    }
}
