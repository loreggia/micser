namespace Micser.Plugins.Main.Dsp;

/// <summary>
/// In-place radix-2 FFT of interleaved complex values (re, im) for sizes up to the one given to the constructor, with precomputed twiddles.
/// Unnormalized: an inverse after a forward transform multiplies by the size.
/// </summary>
internal sealed class Fft
{
    private readonly float[] _cos;
    private readonly int _maxSize;
    private readonly float[] _sin;

    /// <param name="maxSize">The largest size, a power of two.</param>
    public Fft(int maxSize)
    {
        if (maxSize < 2 || !int.IsPow2(maxSize))
        {
            throw new ArgumentOutOfRangeException(nameof(maxSize), maxSize, "The size must be a power of two.");
        }

        _maxSize = maxSize;
        _cos = new float[maxSize / 2];
        _sin = new float[maxSize / 2];
        for (var i = 0; i < maxSize / 2; i++)
        {
            var angle = 2.0 * Math.PI * i / maxSize;
            _cos[i] = (float)Math.Cos(angle);
            _sin[i] = (float)Math.Sin(angle);
        }
    }

    /// <summary>
    /// Transforms the first <paramref name="size"/> complex values of <paramref name="data"/> in place. The forward transform uses
    /// e^(-i), the inverse e^(+i).
    /// </summary>
    /// <param name="size">A power of two up to the maximum given to the constructor.</param>
    public void Transform(Span<float> data, int size, bool inverse)
    {
        if (size < 2 || size > _maxSize || !int.IsPow2(size))
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                "The size must be a power of two up to the maximum."
            );
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(data.Length, 2 * size);

        for (int i = 1, j = 0; i < size; i++)
        {
            var bit = size >> 1;
            for (; (j & bit) != 0; bit >>= 1)
            {
                j ^= bit;
            }

            j ^= bit;
            if (i < j)
            {
                (data[2 * i], data[2 * j]) = (data[2 * j], data[2 * i]);
                (data[(2 * i) + 1], data[(2 * j) + 1]) = (data[(2 * j) + 1], data[(2 * i) + 1]);
            }
        }

        var sign = inverse ? 1f : -1f;
        for (var length = 2; length <= size; length <<= 1)
        {
            var half = length >> 1;
            var stride = _maxSize / length;
            for (var start = 0; start < size; start += length)
            {
                for (var k = 0; k < half; k++)
                {
                    var wr = _cos[k * stride];
                    var wi = sign * _sin[k * stride];
                    var a = 2 * (start + k);
                    var b = 2 * (start + k + half);
                    var tr = (data[b] * wr) - (data[b + 1] * wi);
                    var ti = (data[b] * wi) + (data[b + 1] * wr);
                    data[b] = data[a] - tr;
                    data[b + 1] = data[a + 1] - ti;
                    data[a] += tr;
                    data[a + 1] += ti;
                }
            }
        }
    }
}
