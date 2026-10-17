using System.ComponentModel.DataAnnotations;
using Micser.Audio;
using Micser.Plugins.Main.Dsp;

namespace Micser.Plugins.Main.Modules;

public enum EqualizerBandType
{
    /// <summary>
    /// Raises or lowers the frequencies around <see cref="EqualizerBand.Frequency"/>.
    /// </summary>
    Peaking,

    /// <summary>
    /// Raises or lowers the frequencies below <see cref="EqualizerBand.Frequency"/>.
    /// </summary>
    LowShelf,

    /// <summary>
    /// Raises or lowers the frequencies above <see cref="EqualizerBand.Frequency"/>.
    /// </summary>
    HighShelf,

    /// <summary>
    /// Removes the frequencies above <see cref="EqualizerBand.Frequency"/>, 12 dB per octave; the gain doesn't apply.
    /// </summary>
    LowPass,

    /// <summary>
    /// Removes the frequencies below <see cref="EqualizerBand.Frequency"/>, 12 dB per octave; the gain doesn't apply.
    /// </summary>
    HighPass,
}

/// <param name="Frequency">Center frequency of a peaking band, corner frequency of the others, in Hz.</param>
/// <param name="Gain">Gain in dB at the center frequency or of the shelf; not used by low-pass and high-pass bands.</param>
/// <param name="Q">Quality factor; higher values narrow the band or steepen the corner.</param>
/// <param name="Type">The filter of the band.</param>
public sealed record EqualizerBand(
    [Range(20f, 20000f)] float Frequency,
    [Range(-24f, 24f)] float Gain,
    [Range(0.1f, 20f)] float Q = 1.41f,
    EqualizerBandType Type = EqualizerBandType.Peaking
);

/// <param name="Bands">The bands, applied in order.</param>
/// <param name="Preamp">Gain in dB before the bands, e.g. to make room for boosts.</param>
public sealed record EqualizerState(
    [MaxLength(EqualizerModule.MaxBands)] IReadOnlyList<EqualizerBand> Bands,
    [Range(-24f, 24f)] float Preamp = 0f
);

/// <summary>
/// Parametric equalizer: a preamp and up to <see cref="MaxBands"/> peaking, shelving, low-pass and high-pass bands.
/// </summary>
public class EqualizerModule : EffectModule, IStatefulModule<EqualizerState>
{
    public const int MaxBands = 32;

    private IReadOnlyList<EqualizerBand>? _appliedBands;
    private int _appliedChannels;
    private float _appliedPreamp = float.NaN;

    // MaxBands per channel, grown with the channel count
    private Biquad[][] _filters = [];

    /// <summary>
    /// The bands; replace the list to change them. Filter state is kept while the number of bands stays the same.
    /// </summary>
    public IReadOnlyList<EqualizerBand> Bands { get; set; } = [];

    /// <summary>
    /// Gain in dB before the bands. Changes are ramped over one block.
    /// </summary>
    public float Preamp { get; set; }

    public EqualizerState GetState()
    {
        return new EqualizerState(Bands, Preamp);
    }

    public void SetState(EqualizerState state)
    {
        (Bands, Preamp) = ([.. state.Bands], state.Preamp);
    }

    protected override void Process(AudioBuffer buffer)
    {
        var preamp = Decibels.ToLinear(Preamp);
        buffer.ApplyGain(float.IsNaN(_appliedPreamp) ? preamp : _appliedPreamp, preamp);
        _appliedPreamp = preamp;

        var bands = Bands;
        var bandCount = Math.Min(bands.Count, MaxBands);
        UpdateFilters(bands, bandCount, buffer.ChannelCount);

        for (var c = 0; c < buffer.ChannelCount; c++)
        {
            var channel = buffer.GetChannel(c);
            var filters = _filters[c];
            for (var b = 0; b < bandCount; b++)
            {
                filters[b].Process(channel);
            }
        }
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
            filters[c] = new Biquad[MaxBands];
            for (var b = 0; b < MaxBands; b++)
            {
                filters[c][b] = new Biquad();
            }
        }

        _filters = filters;
    }

    private void UpdateFilters(IReadOnlyList<EqualizerBand> bands, int bandCount, int channelCount)
    {
        if (channelCount == _appliedChannels && ReferenceEquals(bands, _appliedBands))
        {
            return;
        }

        // a new channel or band count starts from a clear state; new settings of the same bands keep it
        var reset = channelCount != _appliedChannels || _appliedBands?.Count != bands.Count;
        EnsureChannels(channelCount);
        for (var c = 0; c < channelCount; c++)
        {
            for (var b = 0; b < bandCount; b++)
            {
                var band = bands[b];
                var filter = _filters[c][b];
                if (reset)
                {
                    filter.Reset();
                }

                filter.Set(band.Type, Format.SampleRate, band.Frequency, band.Q, band.Gain);
            }
        }

        _appliedBands = bands;
        _appliedChannels = channelCount;
    }
}
