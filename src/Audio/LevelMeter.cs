namespace Micser.Audio;

/// <summary>
/// The level of one channel as linear amplitude (1 = full scale).
/// </summary>
/// <param name="Peak">The highest absolute sample value since the previous read.</param>
/// <param name="Rms">The RMS level, smoothed over about 300 ms.</param>
public readonly record struct ChannelLevel(float Peak, float Rms);

/// <param name="Port">The output port, or null for the signal of a module without outputs.</param>
public sealed record PortLevels(string? Port, ChannelLevel[] Channels);

/// <summary>
/// Measures the peak and RMS level of each channel of a signal. <see cref="Measure"/> runs on the audio thread and doesn't lock or allocate;
/// <see cref="Read"/> is called by a single reader on another thread.
/// </summary>
public sealed class LevelMeter
{
    /// <summary>
    /// Channels beyond this are not measured.
    /// </summary>
    public const int MaxChannels = 32;

    private const double RmsTimeConstantSeconds = 0.3;

    private readonly float[] _meanSquares = new float[MaxChannels];

    // non-negative floats compare like their bits as ints, so the maximum can be kept with Interlocked
    private readonly int[] _peakBits = new int[MaxChannels];

    private readonly float _smoothing;
    private long _blocks;
    private int _channelCount;
    private long _readBlocks;

    public LevelMeter(ProcessingFormat format)
    {
        _smoothing = (float)(1 - Math.Exp(-format.BlockDuration.TotalSeconds / RmsTimeConstantSeconds));
    }

    /// <summary>
    /// Adds a block to the measurement.
    /// </summary>
    public void Measure(AudioBuffer buffer)
    {
        var channelCount = Math.Min(buffer.ChannelCount, MaxChannels);
        if (channelCount != _channelCount)
        {
            // the channels mean something else now
            _meanSquares.AsSpan().Clear();
        }

        for (var c = 0; c < channelCount; c++)
        {
            var peak = 0f;
            var sumOfSquares = 0f;
            foreach (var sample in buffer.GetChannel(c))
            {
                var magnitude = MathF.Abs(sample);
                peak = magnitude > peak ? magnitude : peak;
                sumOfSquares += sample * sample;
            }

            // NaN or infinite samples must not stick in the smoothed value
            if (!float.IsFinite(sumOfSquares))
            {
                sumOfSquares = 0;
            }

            peak = MathF.Min(peak, float.MaxValue);
            var meanSquare = Volatile.Read(ref _meanSquares[c]);
            Volatile.Write(
                ref _meanSquares[c],
                meanSquare + (_smoothing * ((sumOfSquares / buffer.FrameCount) - meanSquare))
            );

            var bits = BitConverter.SingleToInt32Bits(peak);
            int current;
            while (
                bits > (current = Volatile.Read(ref _peakBits[c]))
                && Interlocked.CompareExchange(ref _peakBits[c], bits, current) != current
            ) { }
        }

        Volatile.Write(ref _channelCount, channelCount);
        Interlocked.Increment(ref _blocks);
    }

    /// <summary>
    /// Returns the level of each channel and resets the peaks. Returns no channels if no block was measured since the previous read, e.g.
    /// because the module isn't processed.
    /// </summary>
    public ChannelLevel[] Read()
    {
        var blocks = Interlocked.Read(ref _blocks);
        if (blocks == _readBlocks)
        {
            return [];
        }

        _readBlocks = blocks;
        var levels = new ChannelLevel[Volatile.Read(ref _channelCount)];
        for (var c = 0; c < levels.Length; c++)
        {
            var peak = BitConverter.Int32BitsToSingle(Interlocked.Exchange(ref _peakBits[c], 0));
            levels[c] = new ChannelLevel(peak, MathF.Sqrt(Volatile.Read(ref _meanSquares[c])));
        }

        return levels;
    }
}
