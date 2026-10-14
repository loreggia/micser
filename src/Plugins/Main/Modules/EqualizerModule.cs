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
    private float _appliedPreamp = float.NaN;
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
        UpdateFilters(bands, buffer.ChannelCount);

        for (var c = 0; c < buffer.ChannelCount; c++)
        {
            var channel = buffer.GetChannel(c);
            foreach (var filter in _filters[c])
            {
                filter.Process(channel);
            }
        }
    }

    private void UpdateFilters(IReadOnlyList<EqualizerBand> bands, int channelCount)
    {
        if (_filters.Length == channelCount && ReferenceEquals(bands, _appliedBands))
        {
            return;
        }

        if (_filters.Length != channelCount || _appliedBands?.Count != bands.Count)
        {
            _filters = Enumerable
                .Range(0, channelCount)
                .Select(_ => bands.Select(_ => new Biquad()).ToArray())
                .ToArray();
        }

        foreach (var channelFilters in _filters)
        {
            for (var i = 0; i < bands.Count; i++)
            {
                var band = bands[i];
                channelFilters[i].Set(band.Type, Format.SampleRate, band.Frequency, band.Q, band.Gain);
            }
        }

        _appliedBands = bands;
    }
}
