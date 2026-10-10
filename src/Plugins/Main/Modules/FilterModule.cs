using System.ComponentModel.DataAnnotations;
using Micser.Audio;
using NAudio.Dsp;

namespace Micser.Plugins.Main.Modules;

public enum FilterType
{
    /// <summary>
    /// Passes frequencies below the cutoff.
    /// </summary>
    LowPass,

    /// <summary>
    /// Passes frequencies above the cutoff.
    /// </summary>
    HighPass,
}

/// <param name="Frequency">Cutoff frequency in Hz.</param>
/// <param name="Slope">Steepness beyond the cutoff in dB per octave: 12, 24, 36 or 48.</param>
/// <param name="Q">
/// Resonance: the linear gain at the cutoff. The default of 1/√2 (-3 dB) gives a flat Butterworth response, higher values a peak.
/// </param>
public sealed record FilterState(
    FilterType Type = FilterType.HighPass,
    [Range(20f, 20000f)] float Frequency = 80f,
    [AllowedValues(12, 24, 36, 48)] int Slope = 12,
    [Range(0.1f, 10f)] float Q = FilterModule.ButterworthQ
);

/// <summary>
/// Low-pass or high-pass filter: a cascade of biquads, one per 12 dB per octave. The stages have the Q values of a Butterworth filter,
/// except that the one with the highest is scaled by the resonance, so the gain at the cutoff is <see cref="Q"/> at every slope.
/// </summary>
public class FilterModule : EffectModule, IStatefulModule<FilterState>
{
    /// <summary>
    /// The Q of a second-order Butterworth filter, 1/√2.
    /// </summary>
    public const float ButterworthQ = 0.70710677f;

    private const int MaxStages = 4;
    private float _appliedFrequency;
    private float _appliedQ;
    private FilterType _appliedType;
    private BiQuadFilter[][] _filters = [];

    /// <summary>
    /// Cutoff frequency in Hz, limited to below the Nyquist frequency.
    /// </summary>
    public float Frequency { get; set; } = 80f;

    /// <summary>
    /// Resonance: the linear gain at the cutoff.
    /// </summary>
    public float Q { get; set; } = ButterworthQ;

    /// <summary>
    /// Steepness in dB per octave, a multiple of 12 up to 48.
    /// </summary>
    public int Slope { get; set; } = 12;

    public FilterType Type { get; set; } = FilterType.HighPass;

    public FilterState GetState()
    {
        return new FilterState(Type, Frequency, Slope, Q);
    }

    public void SetState(FilterState state)
    {
        Type = state.Type;
        Frequency = state.Frequency;
        Slope = state.Slope;
        Q = state.Q;
    }

    protected override void Process(AudioBuffer buffer)
    {
        var stageCount = Math.Clamp(Slope / 12, 1, MaxStages);
        UpdateFilters(Type, Math.Min(Frequency, Format.SampleRate * 0.45f), Q, stageCount, buffer.ChannelCount);

        for (var c = 0; c < buffer.ChannelCount; c++)
        {
            var channel = buffer.GetChannel(c);
            foreach (var filter in _filters[c])
            {
                filter.Transform(channel, channel);
            }
        }
    }

    /// <summary>
    /// The Q of a biquad in a Butterworth cascade of <paramref name="stageCount"/> biquads, the last (and highest) scaled by
    /// <paramref name="q"/> relative to <see cref="ButterworthQ"/>.
    /// </summary>
    private static float StageQ(int stage, int stageCount, float q)
    {
        var butterworth = 1 / (2 * Math.Cos(((2 * stage) + 1) * Math.PI / (4 * stageCount)));
        return (float)(stage == stageCount - 1 ? butterworth * q / ButterworthQ : butterworth);
    }

    private void UpdateFilters(FilterType type, float frequency, float q, int stageCount, int channelCount)
    {
        var sampleRate = Format.SampleRate;
        if (_filters.Length != channelCount || _filters[0].Length != stageCount)
        {
            _filters = Enumerable
                .Range(0, channelCount)
                .Select(_ =>
                    Enumerable
                        .Range(0, stageCount)
                        .Select(s =>
                            type == FilterType.LowPass
                                ? BiQuadFilter.LowPassFilter(sampleRate, frequency, StageQ(s, stageCount, q))
                                : BiQuadFilter.HighPassFilter(sampleRate, frequency, StageQ(s, stageCount, q))
                        )
                        .ToArray()
                )
                .ToArray();
        }
        else if (type != _appliedType || frequency != _appliedFrequency || q != _appliedQ)
        {
            // Retuning keeps the filter state, so moving the cutoff or the resonance doesn't click.
            foreach (var channelFilters in _filters)
            {
                for (var s = 0; s < stageCount; s++)
                {
                    if (type == FilterType.LowPass)
                    {
                        channelFilters[s].UpdateLowPassFilter(sampleRate, frequency, StageQ(s, stageCount, q));
                    }
                    else
                    {
                        channelFilters[s].UpdateHighPassFilter(sampleRate, frequency, StageQ(s, stageCount, q));
                    }
                }
            }
        }

        _appliedType = type;
        _appliedFrequency = frequency;
        _appliedQ = q;
    }
}
