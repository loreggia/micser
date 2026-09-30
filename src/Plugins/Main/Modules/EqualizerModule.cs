using Micser.Audio;
using NAudio.Dsp;

namespace Micser.Plugins.Main.Modules;

/// <param name="Frequency">Center frequency in Hz.</param>
/// <param name="Gain">Gain at the center frequency in dB.</param>
/// <param name="Q">Quality factor; higher values narrow the band.</param>
public sealed record EqualizerBand(float Frequency, float Gain, float Q = 1.41f);

/// <summary>
/// Parametric equalizer made of peaking filters.
/// </summary>
public class EqualizerModule : EffectModule
{
    private IReadOnlyList<EqualizerBand>? _appliedBands;
    private BiQuadFilter[][] _filters = [];

    /// <summary>
    /// The bands; replace the list to change them. Filter state is kept when only band values change.
    /// </summary>
    public IReadOnlyList<EqualizerBand> Bands { get; set; } = [];

    protected override void Process(AudioBuffer buffer)
    {
        var bands = Bands;
        UpdateFilters(bands, buffer.ChannelCount);

        for (var c = 0; c < buffer.ChannelCount; c++)
        {
            var channel = buffer.GetChannel(c);
            foreach (var filter in _filters[c])
            {
                filter.Transform(channel, channel);
            }
        }
    }

    private void UpdateFilters(IReadOnlyList<EqualizerBand> bands, int channelCount)
    {
        if (_filters.Length == channelCount && ReferenceEquals(bands, _appliedBands))
        {
            return;
        }

        var sampleRate = Format.SampleRate;
        if (_filters.Length != channelCount || _appliedBands?.Count != bands.Count)
        {
            _filters = Enumerable.Range(0, channelCount)
                .Select(_ => bands.Select(b => BiQuadFilter.PeakingEQ(sampleRate, b.Frequency, b.Q, b.Gain)).ToArray())
                .ToArray();
        }
        else
        {
            foreach (var channelFilters in _filters)
            {
                for (var i = 0; i < bands.Count; i++)
                {
                    channelFilters[i].SetPeakingEq(sampleRate, bands[i].Frequency, bands[i].Q, bands[i].Gain);
                }
            }
        }

        _appliedBands = bands;
    }
}
