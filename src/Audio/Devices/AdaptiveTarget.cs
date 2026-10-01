namespace Micser.Audio.Devices;

/// <summary>
/// The target fill of a stream's ring buffer, in device frames. Starts at a minimum, grows by a step after a dropout, and shrinks by a step
/// after a long time without dropouts, but not below a level that had a dropout soon after shrinking to it. Updated once per block on the
/// audio thread.
/// </summary>
internal sealed class AdaptiveTarget
{
    private readonly long _holdOffBlocks;
    private readonly double _maximum;
    private readonly long _stableBlocks;
    private readonly double _step;
    private long _blocksSinceDecrease;
    private long _blocksSinceDropout;
    private long _blocksSinceIncrease;
    private double _floor;

    /// <param name="minimum">The initial and lowest target.</param>
    /// <param name="step">How much the target changes at once.</param>
    /// <param name="maximum">The highest target.</param>
    /// <param name="holdOffBlocks">Dropouts this soon after an increase count as the same incident, since the buffer is still refilling.</param>
    /// <param name="stableBlocks">Blocks without dropouts before the target shrinks.</param>
    public AdaptiveTarget(double minimum, double step, double maximum, long holdOffBlocks, long stableBlocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimum);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);
        _floor = minimum;
        _step = step;
        _maximum = Math.Max(maximum, minimum);
        _holdOffBlocks = holdOffBlocks;
        _stableBlocks = stableBlocks;
        _blocksSinceDecrease = stableBlocks;
        _blocksSinceIncrease = holdOffBlocks;
        Value = minimum;
    }

    public double Value { get; private set; }

    /// <summary>
    /// Advances by one block. Returns whether <see cref="Value"/> changed.
    /// </summary>
    /// <param name="dropout">Whether the stream had a dropout since the previous call.</param>
    public bool Update(bool dropout)
    {
        _blocksSinceDecrease++;
        _blocksSinceDropout++;
        _blocksSinceIncrease++;

        if (dropout)
        {
            _blocksSinceDropout = 0;
            if (_blocksSinceIncrease < _holdOffBlocks || Value >= _maximum)
            {
                return false;
            }

            if (_blocksSinceDecrease < _stableBlocks)
            {
                // the level before the last decrease was needed after all
                _floor = Math.Min(Value + _step, _maximum);
            }

            Value = Math.Min(Value + _step, _maximum);
            _blocksSinceIncrease = 0;
            return true;
        }

        if (_blocksSinceDropout >= _stableBlocks && _blocksSinceDecrease >= _stableBlocks && Value > _floor)
        {
            Value = Math.Max(Value - _step, _floor);
            _blocksSinceDecrease = 0;
            return true;
        }

        return false;
    }
}
