namespace Micser.Audio.Devices;

/// <summary>
/// Lock-free ring buffer for exactly one writer thread and one reader thread.
/// </summary>
internal sealed class SampleRingBuffer
{
    private readonly float[] _samples;
    private long _readPosition;
    private long _writePosition;

    public SampleRingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _samples = new float[capacity];
    }

    public int Capacity => _samples.Length;

    public int Count => (int)(Volatile.Read(ref _writePosition) - Volatile.Read(ref _readPosition));

    /// <summary>
    /// Reader only. Drops up to <paramref name="count"/> samples and returns their number.
    /// </summary>
    public int Discard(int count)
    {
        var position = _readPosition;
        count = Math.Min(count, (int)(Volatile.Read(ref _writePosition) - position));
        Volatile.Write(ref _readPosition, position + count);
        return count;
    }

    /// <summary>
    /// Reader only. Reads as many samples as available and returns their number.
    /// </summary>
    public int Read(Span<float> destination)
    {
        var position = _readPosition;
        var count = Math.Min(destination.Length, (int)(Volatile.Read(ref _writePosition) - position));
        var start = (int)(position % Capacity);
        var firstPart = Math.Min(count, Capacity - start);

        _samples.AsSpan(start, firstPart).CopyTo(destination);
        _samples.AsSpan(0, count - firstPart).CopyTo(destination[firstPart..]);

        Volatile.Write(ref _readPosition, position + count);
        return count;
    }

    /// <summary>
    /// Writer only. Writes as many samples as fit and returns their number.
    /// </summary>
    public int Write(ReadOnlySpan<float> samples)
    {
        var position = _writePosition;
        var count = Math.Min(samples.Length, Capacity - (int)(position - Volatile.Read(ref _readPosition)));
        var start = (int)(position % Capacity);
        var firstPart = Math.Min(count, Capacity - start);

        samples[..firstPart].CopyTo(_samples.AsSpan(start));
        samples[firstPart..count].CopyTo(_samples);

        Volatile.Write(ref _writePosition, position + count);
        return count;
    }

    /// <summary>
    /// Writer only. Writes up to <paramref name="count"/> zero samples and returns their number.
    /// </summary>
    public int WriteSilence(int count)
    {
        var position = _writePosition;
        count = Math.Min(count, Capacity - (int)(position - Volatile.Read(ref _readPosition)));
        var start = (int)(position % Capacity);
        var firstPart = Math.Min(count, Capacity - start);

        _samples.AsSpan(start, firstPart).Clear();
        _samples.AsSpan(0, count - firstPart).Clear();

        Volatile.Write(ref _writePosition, position + count);
        return count;
    }
}
