using System.ComponentModel.DataAnnotations;
using Micser.Audio;

namespace Micser.Plugins.Main.Modules;

/// <param name="Threshold">Level in dB at which the gate opens.</param>
/// <param name="Hysteresis">The gate closes this many dB below the threshold.</param>
/// <param name="Range">Gain in dB while the gate is closed.</param>
/// <param name="Attack">Time in seconds the gate takes to open.</param>
/// <param name="Hold">Time in seconds the gate stays open after the level falls below the closing level.</param>
/// <param name="Release">Time in seconds the gate takes to close.</param>
public sealed record NoiseGateState(
    [Range(-100f, 0f)] float Threshold = -50f,
    [Range(0f, 20f)] float Hysteresis = 4f,
    [Range(-100f, 0f)] float Range = -60f,
    [Range(0.0001f, 1f)] float Attack = 0.002f,
    [Range(0f, 2f)] float Hold = 0.05f,
    [Range(0.001f, 5f)] float Release = 0.15f
);

/// <summary>
/// Stereo-linked noise gate: lowers all channels by <see cref="Range"/> while the peak of all of them stays below the closing level
/// (<see cref="Threshold"/> minus <see cref="Hysteresis"/>) for longer than <see cref="Hold"/>.
/// </summary>
public class NoiseGateModule : EffectModule, IStatefulModule<NoiseGateState>
{
    // decay of the peak detector, so that the level doesn't drop between the peaks of low frequencies
    private const float DetectorRelease = 0.02f;

    private float _alphaAttack;
    private float _alphaDetector;
    private float _alphaRelease;
    private float _gainDb;
    private float[] _gains = [];
    private int _holdRemaining;
    private bool _isOpen;
    private float _level;
    private (float Attack, float Release) _timing = (float.NaN, float.NaN);

    /// <summary>
    /// Time in seconds the gate takes to open.
    /// </summary>
    public float Attack { get; set; } = 0.002f;

    /// <summary>
    /// Time in seconds the gate stays open after the level falls below the closing level.
    /// </summary>
    public float Hold { get; set; } = 0.05f;

    /// <summary>
    /// The gate closes this many dB below <see cref="Threshold"/>.
    /// </summary>
    public float Hysteresis { get; set; } = 4f;

    /// <summary>
    /// Gain in dB while the gate is closed.
    /// </summary>
    public float Range { get; set; } = -60f;

    /// <summary>
    /// Time in seconds the gate takes to close.
    /// </summary>
    public float Release { get; set; } = 0.15f;

    /// <summary>
    /// Level in dB at which the gate opens.
    /// </summary>
    public float Threshold { get; set; } = -50f;

    public NoiseGateState GetState()
    {
        return new NoiseGateState(Threshold, Hysteresis, Range, Attack, Hold, Release);
    }

    public void SetState(NoiseGateState state)
    {
        (Threshold, Hysteresis, Range, Attack, Hold, Release) = (
            state.Threshold,
            state.Hysteresis,
            state.Range,
            state.Attack,
            state.Hold,
            state.Release
        );
    }

    protected override void OnAttached()
    {
        _gains = new float[Format.FrameCount];
        _alphaDetector = MathF.Exp(-1f / (Format.SampleRate * DetectorRelease));
    }

    protected override void Process(AudioBuffer buffer)
    {
        var openLevel = Decibels.ToLinear(Threshold);
        var closeLevel = Decibels.ToLinear(Threshold - Math.Max(Hysteresis, 0f));
        var range = Math.Min(Range, 0f);
        var holdFrames = (int)(Math.Max(Hold, 0f) * Format.SampleRate);

        UpdateTiming();

        var gains = _gains.AsSpan();
        gains.Clear();

        for (var c = 0; c < buffer.ChannelCount; c++)
        {
            var channel = buffer.GetChannel(c);
            for (var i = 0; i < channel.Length; i++)
            {
                gains[i] = Math.Max(gains[i], Math.Abs(channel[i]));
            }
        }

        for (var i = 0; i < gains.Length; i++)
        {
            gains[i] = ComputeGain(gains[i], openLevel, closeLevel, range, holdFrames);
        }

        for (var c = 0; c < buffer.ChannelCount; c++)
        {
            var channel = buffer.GetChannel(c);
            for (var i = 0; i < channel.Length; i++)
            {
                channel[i] *= gains[i];
            }
        }
    }

    private float ComputeGain(float peak, float openLevel, float closeLevel, float range, int holdFrames)
    {
        _level = peak > _level ? peak : _level * _alphaDetector;

        if (_level >= openLevel || (_isOpen && _level >= closeLevel))
        {
            _isOpen = true;
            _holdRemaining = holdFrames;
        }
        else if (_holdRemaining > 0)
        {
            _holdRemaining--;
        }
        else
        {
            _isOpen = false;
        }

        // the gain is smoothed in dB, like the compressor's envelope
        var target = _isOpen ? 0f : range;
        var alpha = target > _gainDb ? _alphaAttack : _alphaRelease;
        _gainDb = (alpha * _gainDb) + ((1f - alpha) * target);
        if (Math.Abs(_gainDb - target) < 0.001f)
        {
            _gainDb = target;
        }

        return _gainDb == 0f ? 1f : Decibels.ToLinear(_gainDb);
    }

    private void UpdateTiming()
    {
        var timing = (Attack, Release);
        if (timing == _timing)
        {
            return;
        }

        _timing = timing;
        _alphaAttack = MathF.Exp(-1f / (Format.SampleRate * Math.Max(timing.Attack, 1e-5f)));
        _alphaRelease = MathF.Exp(-1f / (Format.SampleRate * Math.Max(timing.Release, 1e-5f)));
    }
}
