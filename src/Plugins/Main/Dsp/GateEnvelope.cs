using Micser.Audio;

namespace Micser.Plugins.Main.Dsp;

/// <summary>
/// The detector and gain envelope of a gate: open while a peak level is above the threshold, and until it has been below the closing level
/// (threshold minus hysteresis) for the hold time. The gain moves towards the open or closed gain in dB, with the attack time while open
/// and the release time while closed.
/// </summary>
internal sealed class GateEnvelope
{
    // decay of the peak detector, so that the level doesn't drop between the peaks of low frequencies
    private const float DetectorRelease = 0.02f;

    private float _alphaAttack;
    private float _alphaDetector;
    private float _alphaRelease;
    private float _closeLevel;
    private float _gainDb;
    private int _holdFrames;
    private int _holdRemaining;
    private float _level;
    private float _openLevel;
    private int _sampleRate;
    private (float Attack, float Release) _timing = (float.NaN, float.NaN);

    public bool IsOpen { get; private set; }

    /// <summary>
    /// Sets the sample rate and resets the envelope to closed at 0 dB.
    /// </summary>
    public void Reset(int sampleRate)
    {
        _sampleRate = sampleRate;
        _alphaDetector = MathF.Exp(-1f / (sampleRate * DetectorRelease));
        _timing = (float.NaN, float.NaN);
        _gainDb = 0f;
        _level = 0f;
        _holdRemaining = 0;
        IsOpen = false;
    }

    /// <summary>
    /// Takes the parameters for the next block. Levels are in dB, times in seconds.
    /// </summary>
    public void Configure(float threshold, float hysteresis, float hold, float attack, float release)
    {
        _openLevel = Decibels.ToLinear(threshold);
        _closeLevel = Decibels.ToLinear(threshold - Math.Max(hysteresis, 0f));
        _holdFrames = (int)(Math.Max(hold, 0f) * _sampleRate);

        var timing = (attack, release);
        if (timing != _timing)
        {
            _timing = timing;
            _alphaAttack = MathF.Exp(-1f / (_sampleRate * Math.Max(attack, 1e-5f)));
            _alphaRelease = MathF.Exp(-1f / (_sampleRate * Math.Max(release, 1e-5f)));
        }
    }

    /// <summary>
    /// Advances by one frame with its peak level (linear) and returns the linear gain.
    /// </summary>
    public float Next(float peak, float openGainDb, float closedGainDb)
    {
        _level = peak > _level ? peak : _level * _alphaDetector;

        if (_level >= _openLevel || (IsOpen && _level >= _closeLevel))
        {
            IsOpen = true;
            _holdRemaining = _holdFrames;
        }
        else if (_holdRemaining > 0)
        {
            _holdRemaining--;
        }
        else
        {
            IsOpen = false;
        }

        var target = IsOpen ? openGainDb : closedGainDb;
        var alpha = IsOpen ? _alphaAttack : _alphaRelease;
        _gainDb = (alpha * _gainDb) + ((1f - alpha) * target);
        if (Math.Abs(_gainDb - target) < 0.001f)
        {
            _gainDb = target;
        }

        return _gainDb == 0f ? 1f : Decibels.ToLinear(_gainDb);
    }
}
