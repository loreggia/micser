namespace Micser.Audio;

/// <summary>
/// One processing block of planar float samples. The frame count is fixed; the layout can change between blocks.
/// </summary>
public sealed class AudioBuffer
{
    private float[] _samples = [];

    public AudioBuffer(int frameCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameCount);
        FrameCount = frameCount;
    }

    public AudioBuffer(int frameCount, ChannelLayout layout)
        : this(frameCount)
    {
        SetLayout(layout);
    }

    public int ChannelCount => Layout.ChannelCount;

    public int FrameCount { get; }

    public ChannelLayout Layout { get; private set; } = ChannelLayout.None;

    /// <summary>
    /// Multiplies all samples by a gain that changes linearly from <paramref name="startGain"/> to
    /// <paramref name="endGain"/> over the block.
    /// </summary>
    public void ApplyGain(float startGain, float endGain)
    {
        if (startGain == endGain)
        {
            ApplyGain(endGain);
            return;
        }

        var step = (endGain - startGain) / FrameCount;
        for (var c = 0; c < ChannelCount; c++)
        {
            var channel = GetChannel(c);
            var gain = startGain;
            for (var i = 0; i < channel.Length; i++)
            {
                gain += step;
                channel[i] *= gain;
            }
        }
    }

    public void ApplyGain(float gain)
    {
        if (gain == 1f)
        {
            return;
        }

        if (gain == 0f)
        {
            Clear();
            return;
        }

        var samples = _samples.AsSpan(0, ChannelCount * FrameCount);
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] *= gain;
        }
    }

    public void Clear()
    {
        _samples.AsSpan(0, ChannelCount * FrameCount).Clear();
    }

    public void CopyFrom(AudioBuffer source)
    {
        if (source.FrameCount != FrameCount)
        {
            throw new ArgumentException("The frame counts of the buffers differ.", nameof(source));
        }

        SetLayout(source.Layout);
        source._samples.AsSpan(0, ChannelCount * FrameCount).CopyTo(_samples);
    }

    public Span<float> GetChannel(int channel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(channel);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, ChannelCount);
        return _samples.AsSpan(channel * FrameCount, FrameCount);
    }

    /// <summary>
    /// Changes the layout. Sample contents are undefined afterwards. Only allocates when the channel count grows.
    /// </summary>
    public void SetLayout(ChannelLayout layout)
    {
        var length = layout.ChannelCount * FrameCount;
        if (_samples.Length < length)
        {
            _samples = new float[length];
        }

        Layout = layout;
    }
}
