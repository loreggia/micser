using System.ComponentModel.DataAnnotations;
using Micser.Audio;
using Micser.Plugins.Main.Dsp;

namespace Micser.Plugins.Main.Modules;

/// <summary>
/// The preset a voice changer's parameters came from. The UI fills in the parameters; the module processes only those.
/// </summary>
public enum VoiceChangerPreset
{
    /// <summary>
    /// Parameters set by hand.
    /// </summary>
    Custom,

    MaleToFemale,
    FemaleToMale,
    Child,
    Monster,
    Chipmunk,
}

/// <param name="Preset">The preset the parameters came from, <see cref="VoiceChangerPreset.Custom"/> once they were changed.</param>
/// <param name="Pitch">Pitch shift in semitones; the formants stay unless <paramref name="Formant"/> moves them.</param>
/// <param name="Formant">Formant shift in semitones: positive values sound smaller (female, child), negative larger (male, giant).</param>
/// <param name="Mix">0 (dry) .. 1 (only the changed voice).</param>
/// <param name="Quality">1..10; higher values use larger FFTs and more overlap: better quality, more latency and CPU.</param>
public sealed record VoiceChangerState(
    VoiceChangerPreset Preset = VoiceChangerPreset.Custom,
    [Range(-24f, 24f)] float Pitch = 0f,
    [Range(-12f, 12f)] float Formant = 0f,
    [Range(0f, 1f)] float Mix = 1f,
    [Range(1, 10)] int Quality = 6
);

/// <summary>
/// Changes a voice by shifting its pitch and its formants independently (STFT-based, one shifter per channel). The parameters are
/// replaced as a whole, so a preset applies in one block.
/// </summary>
public class VoiceChangerModule : EffectModule, IStatefulModule<VoiceChangerState>
{
    private const int MaxFftSize = 4096;
    private int _appliedChannels;

    // one per channel, grown with the channel count
    private VoiceShifter[] _shifters = [];
    private VoiceChangerState _state = new();

    public VoiceChangerState GetState()
    {
        return _state;
    }

    public void SetState(VoiceChangerState state)
    {
        _state = state;
    }

    /// <summary>
    /// The FFT size and oversampling for a quality value: 512 to 2048 at 48 kHz, doubled from 88.2 kHz so the frames keep their duration.
    /// </summary>
    public static (int FftSize, int Oversampling) GetFftSettings(int quality, int sampleRate)
    {
        var (fftSize, oversampling) = Math.Clamp(quality, 1, 10) switch
        {
            <= 3 => (512, 4),
            <= 6 => (1024, 4),
            <= 8 => (1024, 8),
            _ => (2048, 8),
        };

        return (sampleRate >= 88200 ? fftSize * 2 : fftSize, oversampling);
    }

    /// <summary>
    /// The frequency factor for a shift in semitones.
    /// </summary>
    public static float GetFactor(float semitones)
    {
        return MathF.Pow(2f, semitones / 12f);
    }

    protected override void Process(AudioBuffer buffer)
    {
        var state = _state;
        if ((state.Pitch == 0f && state.Formant == 0f) || state.Mix <= 0f)
        {
            return;
        }

        var (fftSize, oversampling) = GetFftSettings(state.Quality, Format.SampleRate);
        var channelCount = buffer.ChannelCount;
        EnsureChannels(channelCount);

        // the shifters reset themselves when the FFT size changes
        if (channelCount != _appliedChannels)
        {
            _appliedChannels = channelCount;
            for (var c = 0; c < channelCount; c++)
            {
                _shifters[c].Reset();
            }
        }

        var pitch = GetFactor(Math.Clamp(state.Pitch, -24f, 24f));
        var formant = GetFactor(Math.Clamp(state.Formant, -12f, 12f));
        var mix = Math.Clamp(state.Mix, 0f, 1f);
        for (var c = 0; c < channelCount; c++)
        {
            _shifters[c].Process(pitch, formant, mix, fftSize, oversampling, Format.SampleRate, buffer.GetChannel(c));
        }
    }

    /// <summary>
    /// Makes room for the channels. Allocates only when the channel count grows beyond all earlier ones.
    /// </summary>
    private void EnsureChannels(int channelCount)
    {
        if (_shifters.Length >= channelCount)
        {
            return;
        }

        var shifters = new VoiceShifter[channelCount];
        _shifters.CopyTo(shifters, 0);
        for (var c = _shifters.Length; c < channelCount; c++)
        {
            shifters[c] = new VoiceShifter(MaxFftSize);
        }

        _shifters = shifters;
    }
}
