using System.ComponentModel.DataAnnotations;
using Micser.Audio;
using Micser.Plugins.Main.Dsp;

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
    private int _appliedChannels;
    private float _appliedFrequency;
    private float _appliedQ;
    private int _appliedStages;
    private FilterType _appliedType;

    // MaxStages per channel, grown with the channel count
    private Biquad[][] _filters = [];

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
            var filters = _filters[c];
            for (var s = 0; s < stageCount; s++)
            {
                filters[s].Process(channel);
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

    /// <summary>
    /// Makes room for the channels. Allocates only when the channel count grows beyond all earlier ones.
    /// </summary>
    private void EnsureChannels(int channelCount)
    {
        if (_filters.Length >= channelCount)
        {
            return;
        }

        var filters = new Biquad[channelCount][];
        _filters.CopyTo(filters, 0);
        for (var c = _filters.Length; c < channelCount; c++)
        {
            filters[c] = new Biquad[MaxStages];
            for (var s = 0; s < MaxStages; s++)
            {
                filters[c][s] = new Biquad();
            }
        }

        _filters = filters;
    }

    private void UpdateFilters(FilterType type, float frequency, float q, int stageCount, int channelCount)
    {
        // A new channel count or slope starts from a clear state. Retuning keeps it, so moving the cutoff or the resonance doesn't click.
        var reset = channelCount != _appliedChannels || stageCount != _appliedStages;
        if (!reset && type == _appliedType && frequency == _appliedFrequency && q == _appliedQ)
        {
            return;
        }

        EnsureChannels(channelCount);
        var bandType = type == FilterType.LowPass ? EqualizerBandType.LowPass : EqualizerBandType.HighPass;
        for (var c = 0; c < channelCount; c++)
        {
            for (var s = 0; s < stageCount; s++)
            {
                var filter = _filters[c][s];
                if (reset)
                {
                    filter.Reset();
                }

                filter.Set(bandType, Format.SampleRate, frequency, StageQ(s, stageCount, q), 0f);
            }
        }

        _appliedChannels = channelCount;
        _appliedStages = stageCount;
        _appliedType = type;
        _appliedFrequency = frequency;
        _appliedQ = q;
    }
}
