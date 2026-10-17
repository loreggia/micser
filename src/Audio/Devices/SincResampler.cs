namespace Micser.Audio.Devices;

/// <summary>
/// A windowed-sinc resampler (64 taps, Blackman-Harris window, 32-fold interpolated) for interleaved float samples. NAudio's
/// <c>WdlResampler</c> in sinc mode, reduced to that mode and a fixed channel count, with an input buffer that only grows: NAudio's resizes
/// it to the exact size on every call, which allocates on almost every block while the drift correction changes the ratio.
/// </summary>
/// <remarks>
/// Altered from NAudio's WdlResampler (MIT, Mark Heath), a port of the resampler in Cockos WDL:
/// Copyright (C) 2005 and later Cockos Incorporated. This software is provided 'as-is', without any express or implied warranty. In no
/// event will the authors be held liable for any damages arising from the use of this software. Permission is granted to anyone to use
/// this software for any purpose, including commercial applications, and to alter it and redistribute it freely, subject to the following
/// restrictions: 1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If
/// you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
/// 2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software. 3. This
/// notice may not be removed or altered from any source distribution.
/// </remarks>
internal sealed class SincResampler
{
    private const int Oversize = 32;
    private const int SincSize = 64;

    private readonly int _channels;
    private readonly float[] _filter = new float[(SincSize + 1) * Oversize];
    private readonly bool _inputDriven;
    private float[] _buffer = [];
    private int _bufferedFrames;
    private double _filterRatio = -1;
    private double _fracPos;
    private int _lastRequested;
    private double _ratio = 1;
    private double _rateIn = 44100;
    private double _rateOut = 44100;

    /// <param name="inputDriven">Whether <see cref="Prepare"/> gets the input frames there are instead of the output frames wanted.</param>
    public SincResampler(int channels, bool inputDriven)
    {
        _channels = channels;
        _inputDriven = inputDriven;
    }

    /// <summary>
    /// Makes room for the next input and returns where to write it. Write the returned number of frames (or fewer, which flushes) and call
    /// <see cref="Resample"/>.
    /// </summary>
    /// <param name="frames">The output frames wanted, or with <c>inputDriven</c> the input frames there are.</param>
    public int Prepare(int frames, out Span<float> input)
    {
        const int HalfSize = SincSize / 2;
        if (_bufferedFrames < HalfSize - 1)
        {
            // the filter's latency, as silence
            EnsureCapacity((HalfSize - 1) * _channels);
            _buffer.AsSpan(0, (HalfSize - 1) * _channels).Clear();
            _bufferedFrames = HalfSize - 1;
        }

        var requested = _inputDriven ? frames : (int)(_ratio * frames) + 4 + SincSize - _bufferedFrames;
        requested = Math.Max(requested, 0);
        EnsureCapacity((_bufferedFrames + requested) * _channels);

        input = _buffer.AsSpan(_bufferedFrames * _channels, requested * _channels);
        _lastRequested = requested;
        return requested;
    }

    /// <summary>
    /// Converts the buffered input into <paramref name="output"/> and returns the output frames produced. With fewer input frames than
    /// <see cref="Prepare"/> asked for, the input is flushed to produce all remaining valid frames.
    /// </summary>
    public int Resample(Span<float> output, int inputFrames, int outputFrames)
    {
        _bufferedFrames += Math.Min(inputFrames, _lastRequested);
        var available = _bufferedFrames;

        if (inputFrames < _lastRequested)
        {
            var padding = ((_lastRequested - inputFrames) * 2) + (SincSize * 2);
            EnsureCapacity((_bufferedFrames + padding) * _channels);
            _buffer.AsSpan(_bufferedFrames * _channels, padding * _channels).Clear();
            available = _bufferedFrames + padding;
        }

        BuildLowPass(_ratio > 1.0 ? 1.0 / (_ratio * 1.03) : 1.0);

        var produced = 0;
        var position = _fracPos;
        var filterLength = available - SincSize;
        var outputLatency = (SincSize / 2) - 1;
        for (var outIndex = 0; produced < outputFrames; outIndex += _channels)
        {
            var index = (int)position;
            if (index >= filterLength - 1)
            {
                // not enough input
                break;
            }

            SincSample(output, outIndex, index * _channels, position - index);
            position += _ratio;
            produced++;
        }

        if (produced > 0 && available > _bufferedFrames)
        {
            // drop the frames that came from the padding, and rewind to the input actually used (as NAudio, unlike WDL)
            var excess = (position - _bufferedFrames + outputLatency) / _ratio;
            if (excess > 0)
            {
                var trimmed = Math.Min((int)(excess + 0.5), produced);
                produced -= trimmed;
                position = Math.Max(position - (trimmed * _ratio), 0);
            }
        }

        var consumed = Math.Min((int)position, _bufferedFrames);
        _fracPos = position - consumed;
        _bufferedFrames -= consumed;
        if (_bufferedFrames <= 0)
        {
            _bufferedFrames = 0;
        }
        else
        {
            _buffer.AsSpan(consumed * _channels, _bufferedFrames * _channels).CopyTo(_buffer);
        }

        return produced;
    }

    public void SetRates(double rateIn, double rateOut)
    {
        rateIn = Math.Max(rateIn, 1.0);
        rateOut = Math.Max(rateOut, 1.0);
        if (rateIn != _rateIn || rateOut != _rateOut)
        {
            _rateIn = rateIn;
            _rateOut = rateOut;
            _ratio = rateIn / rateOut;
        }
    }

    private void BuildLowPass(double filterPosition)
    {
        if (_filterRatio == filterPosition)
        {
            return;
        }

        _filterRatio = filterPosition;
        const int Size = SincSize * Oversize;
        const int HalfSize = Size / 2;
        var power = 0.0;
        var windowPosition = 0.0;
        var windowStep = 2.0 * Math.PI / Size;
        var sincStep = Math.PI / Oversize * filterPosition;
        var sincPosition = sincStep * -HalfSize;

        for (var x = -HalfSize; x < HalfSize + Oversize; x++)
        {
            // Blackman-Harris
            var value =
                0.35875
                - (0.48829 * Math.Cos(windowPosition))
                + (0.14128 * Math.Cos(2 * windowPosition))
                - (0.01168 * Math.Cos(3 * windowPosition));
            if (x != 0)
            {
                value *= Math.Sin(sincPosition) / sincPosition;
            }

            windowPosition += windowStep;
            sincPosition += sincStep;
            _filter[HalfSize + x] = (float)value;
            if (x < HalfSize)
            {
                power += value;
            }
        }

        power = Oversize / power;
        for (var x = 0; x < Size + Oversize; x++)
        {
            _filter[x] = (float)(_filter[x] * power);
        }
    }

    /// <summary>
    /// Grows the buffer, keeping its contents. Allocates only until it has seen the largest request.
    /// </summary>
    private void EnsureCapacity(int samples)
    {
        if (_buffer.Length < samples)
        {
            Array.Resize(ref _buffer, Math.Max(samples, _buffer.Length * 2));
        }
    }

    private void SincSample(Span<float> output, int outIndex, int inIndex, double fracPos)
    {
        fracPos *= Oversize;
        var offset = (int)fracPos;
        var filterIndex = Oversize - 1 - offset;
        fracPos -= offset;

        for (var c = 0; c < _channels; c++)
        {
            double sum = 0.0,
                sum2 = 0.0;
            var f = filterIndex;
            var i = inIndex + c;
            for (var tap = 0; tap < SincSize; tap++)
            {
                sum += _filter[f] * _buffer[i];
                sum2 += _filter[f + 1] * _buffer[i];
                i += _channels;
                f += Oversize;
            }

            output[outIndex + c] = (float)((sum * fracPos) + (sum2 * (1.0 - fracPos)));
        }
    }
}
