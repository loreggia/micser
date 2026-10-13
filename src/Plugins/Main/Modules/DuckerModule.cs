using System.ComponentModel.DataAnnotations;
using Micser.Audio;
using Micser.Plugins.Main.Dsp;

namespace Micser.Plugins.Main.Modules;

/// <param name="Threshold">Level in dB of the sidechain above which the input is lowered.</param>
/// <param name="Depth">Gain in dB of the input while the sidechain is above the threshold.</param>
/// <param name="Attack">Time in seconds the input takes to go down.</param>
/// <param name="Hold">Time in seconds the input stays down after the sidechain falls below the threshold.</param>
/// <param name="Release">Time in seconds the input takes to come back.</param>
public sealed record DuckerState(
    [Range(-80f, 0f)] float Threshold = -40f,
    [Range(-60f, 0f)] float Depth = -15f,
    [Range(0.0001f, 1f)] float Attack = 0.01f,
    [Range(0f, 2f)] float Hold = 0.3f,
    [Range(0.001f, 5f)] float Release = 0.5f
);

/// <summary>
/// Lowers the input by <see cref="Depth"/> while the peak of all channels of the sidechain is above <see cref="Threshold"/>, e.g. music
/// while someone speaks. Without sidechain connections the input passes unchanged.
/// </summary>
public class DuckerModule : EffectModule, IStatefulModule<DuckerState>
{
    // the sidechain counts as quiet this many dB below the threshold, so that it doesn't flicker around it
    private const float Hysteresis = 3f;

    private readonly GateEnvelope _envelope = new();
    private float[] _gains = [];

    public DuckerModule()
    {
        Sidechain = AddInput("Sidechain");
    }

    /// <summary>
    /// Time in seconds the input takes to go down.
    /// </summary>
    public float Attack { get; set; } = 0.01f;

    /// <summary>
    /// Gain in dB of the input while the sidechain is above the threshold.
    /// </summary>
    public float Depth { get; set; } = -15f;

    /// <summary>
    /// Time in seconds the input stays down after the sidechain falls below the threshold.
    /// </summary>
    public float Hold { get; set; } = 0.3f;

    /// <summary>
    /// Time in seconds the input takes to come back.
    /// </summary>
    public float Release { get; set; } = 0.5f;

    /// <summary>
    /// The signal that lowers the input.
    /// </summary>
    public InputPort Sidechain { get; }

    /// <summary>
    /// Level in dB of the sidechain above which the input is lowered.
    /// </summary>
    public float Threshold { get; set; } = -40f;

    public DuckerState GetState()
    {
        return new DuckerState(Threshold, Depth, Attack, Hold, Release);
    }

    public void SetState(DuckerState state)
    {
        (Threshold, Depth, Attack, Hold, Release) = (
            state.Threshold,
            state.Depth,
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
        var depth = Math.Min(Depth, 0f);
        _envelope.Configure(Threshold, Hysteresis, Hold, Attack, Release);

        var gains = _gains.AsSpan();
        Sidechain.Buffer.ComputePeaks(gains);

        for (var i = 0; i < gains.Length; i++)
        {
            gains[i] = _envelope.Next(gains[i], depth, 0f);
        }

        buffer.ApplyGains(gains);
    }
}
