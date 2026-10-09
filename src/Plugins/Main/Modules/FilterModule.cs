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

/// <param name="Frequency">Cutoff frequency in Hz, where the response is 3 dB down.</param>
/// <param name="Slope">Steepness beyond the cutoff in dB per octave: 12, 24, 36 or 48.</param>
public sealed record FilterState(
    FilterType Type = FilterType.HighPass,
    [Range(20f, 20000f)] float Frequency = 80f,
    [AllowedValues(12, 24, 36, 48)] int Slope = 12
);

/// <summary>
/// Butterworth low-pass or high-pass filter: a cascade of biquads, one per 12 dB per octave.
/// </summary>
public class FilterModule : EffectModule, IStatefulModule<FilterState>
{
    private const int MaxStages = 4;
    private float _appliedFrequency;
    private FilterType _appliedType;
    private BiQuadFilter[][] _filters = [];

    /// <summary>
    /// Cutoff frequency in Hz, limited to below the Nyquist frequency.
    /// </summary>
    public float Frequency { get; set; } = 80f;

    /// <summary>
    /// Steepness in dB per octave, a multiple of 12 up to 48.
    /// </summary>
    public int Slope { get; set; } = 12;

    public FilterType Type { get; set; } = FilterType.HighPass;

    public FilterState GetState()
    {
        return new FilterState(Type, Frequency, Slope);
    }

    public void SetState(FilterState state)
    {
        Type = state.Type;
        Frequency = state.Frequency;
        Slope = state.Slope;
    }

    protected override void Process(AudioBuffer buffer)
    {
        var stageCount = Math.Clamp(Slope / 12, 1, MaxStages);
        UpdateFilters(Type, Math.Min(Frequency, Format.SampleRate * 0.45f), stageCount, buffer.ChannelCount);

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
    /// The Q of each biquad in a Butterworth cascade of <paramref name="stageCount"/> biquads.
    /// </summary>
    private static float StageQ(int stage, int stageCount)
    {
        return (float)(1 / (2 * Math.Cos(((2 * stage) + 1) * Math.PI / (4 * stageCount))));
    }

    private void UpdateFilters(FilterType type, float frequency, int stageCount, int channelCount)
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
                                ? BiQuadFilter.LowPassFilter(sampleRate, frequency, StageQ(s, stageCount))
                                : BiQuadFilter.HighPassFilter(sampleRate, frequency, StageQ(s, stageCount))
                        )
                        .ToArray()
                )
                .ToArray();
        }
        else if (type != _appliedType || frequency != _appliedFrequency)
        {
            // Retuning keeps the filter state, so moving the cutoff doesn't click.
            foreach (var channelFilters in _filters)
            {
                for (var s = 0; s < stageCount; s++)
                {
                    if (type == FilterType.LowPass)
                    {
                        channelFilters[s].UpdateLowPassFilter(sampleRate, frequency, StageQ(s, stageCount));
                    }
                    else
                    {
                        channelFilters[s].UpdateHighPassFilter(sampleRate, frequency, StageQ(s, stageCount));
                    }
                }
            }
        }

        _appliedType = type;
        _appliedFrequency = frequency;
    }
}
