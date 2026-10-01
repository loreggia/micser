using System.ComponentModel.DataAnnotations;
using Micser.Audio;
using Micser.Plugins.Main.Dsp;

namespace Micser.Plugins.Main.Modules;

public enum CompressorType
{
    /// <summary>
    /// Reduces levels above the threshold.
    /// </summary>
    Downward,

    /// <summary>
    /// Raises levels below the threshold.
    /// </summary>
    Upward,
}

/// <param name="Amount">Blend of the compression effect, 0..1.</param>
/// <param name="Attack">Attack time in seconds.</param>
/// <param name="Release">Release time in seconds.</param>
/// <param name="Threshold">Threshold in dB.</param>
/// <param name="Knee">Soft knee width in dB.</param>
/// <param name="MakeUpGain">Make-up gain in dB, scaled by <paramref name="Amount"/>.</param>
public sealed record CompressorState(
    CompressorType Type = CompressorType.Downward,
    [Range(0f, 1f)] float Amount = 1f,
    [Range(0.0001f, 1f)] float Attack = 0.01f,
    [Range(0.001f, 5f)] float Release = 0.1f,
    [Range(1f, 20f)] float Ratio = 2f,
    [Range(-80f, 0f)] float Threshold = -10f,
    [Range(0f, 24f)] float Knee = 5f,
    [Range(-24f, 24f)] float MakeUpGain = 0f);

/// <summary>
/// Stereo-linked compressor: the gain is computed from the peak of all channels and applied to all of them.
/// Based on the compressor in "Audio Effects: Theory, Implementation and Application" (Reiss, McPherson).
/// </summary>
public class CompressorModule : EffectModule, IStatefulModule<CompressorState>
{
    private const int ChunkFrames = 32;
    private float _alphaAttack;
    private float _alphaRelease;
    private CompressorType _appliedType;
    private float _chunkMaxDiff;
    private int _chunkPosition;
    private float _envelope;
    private float[] _gains = [];
    private (float Attack, float Release) _timing = (float.NaN, float.NaN);

    /// <summary>
    /// Blend of the compression effect, 0..1.
    /// </summary>
    public float Amount { get; set; } = 1f;

    /// <summary>
    /// Attack time in seconds.
    /// </summary>
    public float Attack { get; set; } = 0.01f;

    /// <summary>
    /// Soft knee width in dB.
    /// </summary>
    public float Knee { get; set; } = 5f;

    /// <summary>
    /// Make-up gain in dB, scaled by <see cref="Amount"/>.
    /// </summary>
    public float MakeUpGain { get; set; }

    public float Ratio { get; set; } = 2f;

    /// <summary>
    /// Release time in seconds.
    /// </summary>
    public float Release { get; set; } = 0.1f;

    /// <summary>
    /// Threshold in dB.
    /// </summary>
    public float Threshold { get; set; } = -10f;

    public CompressorType Type { get; set; } = CompressorType.Downward;

    public CompressorState GetState()
    {
        return new CompressorState(Type, Amount, Attack, Release, Ratio, Threshold, Knee, MakeUpGain);
    }

    public void SetState(CompressorState state)
    {
        (Type, Amount, Attack, Release, Ratio, Threshold, Knee, MakeUpGain) =
            (state.Type, state.Amount, state.Attack, state.Release, state.Ratio, state.Threshold, state.Knee, state.MakeUpGain);
    }

    protected override void OnAttached()
    {
        _gains = new float[Format.FrameCount];
    }

    protected override void Process(AudioBuffer buffer)
    {
        var type = Type;
        var amount = Amount;
        var ratio = Ratio;
        var threshold = Threshold;
        var knee = Knee;
        var makeUpGain = MakeUpGain;

        if (type != _appliedType)
        {
            _appliedType = type;
            _envelope = 0f;
        }

        UpdateTiming();

        if (ratio <= 1f)
        {
            return;
        }

        var slope = 1f / ratio;
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
            gains[i] = ComputeGain(gains[i], type, slope, threshold, knee, amount, makeUpGain);
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

    private float ComputeGain(float level, CompressorType type, float slope, float threshold, float knee, float amount, float makeUpGain)
    {
        // exact silence keeps the envelope, otherwise the gain jumps when audio starts after a pause
        if (level < float.Epsilon)
        {
            return 1f;
        }

        var levelDb = level < 0.000001f ? -120f : Decibels.FromLinear(level);
        var compressedDb = type == CompressorType.Downward
            ? CompressorCurve.Downward(levelDb, slope, threshold, knee)
            : CompressorCurve.Upward(levelDb, slope, threshold, knee);
        var diff = (levelDb - compressedDb) * amount;

        _chunkMaxDiff = _chunkPosition == 0 ? diff : Math.Max(_chunkMaxDiff, diff);
        _chunkPosition = (_chunkPosition + 1) % ChunkFrames;

        var isAttack = type == CompressorType.Downward ? _chunkMaxDiff > _envelope : _chunkMaxDiff < _envelope;
        var alpha = isAttack ? _alphaAttack : _alphaRelease;
        _envelope = alpha * _envelope + (1f - alpha) * _chunkMaxDiff;

        return Decibels.ToLinear(makeUpGain * amount - _envelope);
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
