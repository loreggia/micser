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

/// <summary>
/// Stereo-linked compressor: the gain is computed from the peak of all channels and applied to all of them.
/// Based on the compressor in "Audio Effects: Theory, Implementation and Application" (Reiss, McPherson).
/// </summary>
public class CompressorModule : EffectModule
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
