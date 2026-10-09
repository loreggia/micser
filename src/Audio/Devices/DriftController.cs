namespace Micser.Audio.Devices;

/// <summary>
/// Keeps a ring buffer at its target fill level between two clocks by slightly adjusting the resampling ratio.
/// A PI controller on the smoothed fill level; the correction is limited so it stays inaudible.
/// </summary>
internal sealed class DriftController
{
    private const double IntegralGain = 0.000002;
    private const double ProportionalGain = 0.004;
    private const double Smoothing = 0.02;
    private readonly double _maxCorrection;
    private double _integral;
    private double _smoothedFill;

    public DriftController(double targetFill, double maxCorrection = 0.005)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetFill);
        TargetFill = targetFill;
        _maxCorrection = maxCorrection;
        Reset();
    }

    /// <summary>
    /// The last correction factor: above 1 when the buffer is too full, below 1 when it's too empty.
    /// </summary>
    public double Correction { get; private set; } = 1d;

    public double SmoothedFill => _smoothedFill;

    /// <summary>
    /// The fill level to keep, in frames. Changes are followed gradually through the correction.
    /// </summary>
    public double TargetFill
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    }

    /// <summary>
    /// Restarts from the target fill level, keeping the learned clock offset.
    /// </summary>
    public void Reset()
    {
        _smoothedFill = TargetFill;
    }

    /// <summary>
    /// Updates the controller with the current fill level (once per block) and returns the new correction factor.
    /// </summary>
    public double Update(double fill)
    {
        _smoothedFill += Smoothing * (fill - _smoothedFill);

        var error = (_smoothedFill - TargetFill) / TargetFill;
        _integral = Math.Clamp(_integral + (IntegralGain * error), -_maxCorrection, _maxCorrection);

        Correction = 1d + Math.Clamp((ProportionalGain * error) + _integral, -_maxCorrection, _maxCorrection);
        return Correction;
    }
}
