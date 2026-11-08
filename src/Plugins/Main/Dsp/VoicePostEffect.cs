using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Dsp;

/// <summary>
/// The voice changer's effects on the whole output of a channel: a ring modulator (<see cref="VoiceEffect.Alien"/>) and a band-pass
/// with soft clipping (<see cref="VoiceEffect.Radio"/>).
/// </summary>
internal sealed class VoicePostEffect
{
    /// <summary>
    /// The carrier of the ring modulator in Hz.
    /// </summary>
    public const double AlienCarrier = 220.0;

    /// <summary>
    /// The band of the radio, about that of a telephone line, in Hz.
    /// </summary>
    public const float RadioHighPass = 300f;

    public const float RadioLowPass = 3400f;

    /// <summary>
    /// The share of the ring-modulated signal; the rest is the signal as it is.
    /// </summary>
    private const float AlienDepth = 0.6f;

    private readonly Biquad _highPass = new();
    private readonly Biquad _lowPass = new();
    private VoiceEffect _appliedEffect;
    private int _appliedSampleRate;
    private double _carrierPhase;

    public void Reset()
    {
        _highPass.Reset();
        _lowPass.Reset();
        _carrierPhase = 0;
        _appliedEffect = VoiceEffect.None;
    }

    /// <summary>
    /// Applies <paramref name="effect"/> to <paramref name="samples"/> in place; other effects leave them unchanged. Switching to an effect
    /// starts it from a clean state.
    /// </summary>
    public void Process(VoiceEffect effect, int sampleRate, Span<float> samples)
    {
        if (effect != _appliedEffect)
        {
            Reset();
            _appliedEffect = effect;
        }

        switch (effect)
        {
            case VoiceEffect.Alien:
                ProcessAlien(sampleRate, samples);
                break;
            case VoiceEffect.Radio:
                ProcessRadio(sampleRate, samples);
                break;
            case VoiceEffect.None:
            case VoiceEffect.Robot:
            case VoiceEffect.Whisper:
            default:
                break;
        }
    }

    private void ProcessAlien(int sampleRate, Span<float> samples)
    {
        var increment = 2.0 * Math.PI * AlienCarrier / sampleRate;
        var phase = _carrierPhase;
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] *= 1f - AlienDepth + (AlienDepth * (float)Math.Cos(phase));
            phase += increment;
        }

        _carrierPhase = Math.IEEERemainder(phase, 2.0 * Math.PI);
    }

    private void ProcessRadio(int sampleRate, Span<float> samples)
    {
        if (sampleRate != _appliedSampleRate)
        {
            _appliedSampleRate = sampleRate;
            _highPass.Set(EqualizerBandType.HighPass, sampleRate, RadioHighPass, 0.71f, 0f);
            _lowPass.Set(EqualizerBandType.LowPass, sampleRate, RadioLowPass, 0.71f, 0f);
        }

        _highPass.Process(samples);
        _lowPass.Process(samples);

        // unity gain for quiet signals, saturating towards 0.5
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = 0.5f * MathF.Tanh(2f * samples[i]);
        }
    }
}
