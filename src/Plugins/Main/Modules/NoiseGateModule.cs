using System.ComponentModel.DataAnnotations;
using Micser.Audio;
using Micser.Plugins.Main.Dsp;

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
    private readonly GateEnvelope _envelope = new();
    private float[] _gains = [];

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
        _envelope.Reset(Format.SampleRate);
    }

    protected override void Process(AudioBuffer buffer)
    {
        var range = Math.Min(Range, 0f);
        _envelope.Configure(Threshold, Hysteresis, Hold, Attack, Release);

        var gains = _gains.AsSpan();
        buffer.ComputePeaks(gains);

        for (var i = 0; i < gains.Length; i++)
        {
            gains[i] = _envelope.Next(gains[i], 0f, range);
        }

        buffer.ApplyGains(gains);
    }
}
