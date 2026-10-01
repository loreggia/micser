using System.ComponentModel.DataAnnotations;
using Micser.Audio;
using NAudio.Dsp;

namespace Micser.Plugins.Main.Modules;

/// <param name="Pitch">-1 (one octave down) .. 0 (unchanged) .. 1 (one octave up).</param>
/// <param name="Quality">1..10; higher values use larger FFTs and more overlap: better quality, more latency and CPU.</param>
public sealed record PitchState([Range(-1f, 1f)] float Pitch = 0f, [Range(1, 10)] int Quality = 4);

/// <summary>
/// Shifts the pitch without changing the duration (STFT-based, one shifter per channel).
/// </summary>
public class PitchModule : EffectModule, IStatefulModule<PitchState>
{
    private (int Channels, int FftSize) _appliedSettings;
    private SmbPitchShifter[] _shifters = [];

    /// <summary>
    /// -1 (one octave down) .. 0 (unchanged) .. 1 (one octave up).
    /// </summary>
    public float Pitch { get; set; }

    /// <summary>
    /// 1..10; higher values use larger FFTs and more overlap: better quality, more latency and CPU.
    /// </summary>
    public int Quality { get; set; } = 4;

    public PitchState GetState()
    {
        return new PitchState(Pitch, Quality);
    }

    public void SetState(PitchState state)
    {
        (Pitch, Quality) = (state.Pitch, state.Quality);
    }

    /// <summary>
    /// The FFT size (256..4096) and oversampling (4..8) for a quality value.
    /// </summary>
    public static (int FftSize, int Oversampling) GetFftSettings(int quality)
    {
        var t = (Math.Clamp(quality, 1, 10) - 1) / 9f;
        var fftSize = 1 << (int)MathF.Round(8 + 4 * t);
        var oversampling = (int)MathF.Round(4 + 4 * t);
        return (fftSize, oversampling);
    }

    /// <summary>
    /// The frequency factor for a pitch value: 0.5 at -1, 1 at 0, 2 at 1.
    /// </summary>
    public static float GetPitchFactor(float pitch)
    {
        return MathF.Pow(2f, Math.Clamp(pitch, -1f, 1f));
    }

    protected override void Process(AudioBuffer buffer)
    {
        var pitch = Pitch;
        if (pitch == 0f)
        {
            return;
        }

        var (fftSize, oversampling) = GetFftSettings(Quality);
        if (_appliedSettings != (buffer.ChannelCount, fftSize))
        {
            _appliedSettings = (buffer.ChannelCount, fftSize);
            _shifters = Enumerable.Range(0, buffer.ChannelCount).Select(_ => new SmbPitchShifter()).ToArray();
        }

        var factor = GetPitchFactor(pitch);
        for (var c = 0; c < buffer.ChannelCount; c++)
        {
            _shifters[c].PitchShift(factor, buffer.FrameCount, fftSize, oversampling, Format.SampleRate, buffer.GetChannel(c));
        }
    }
}
