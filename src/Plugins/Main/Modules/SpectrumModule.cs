using Micser.Audio;
using NAudio.Dsp;

namespace Micser.Plugins.Main.Modules;

/// <param name="FrequencyResolution">Width of one bin in Hz; bin <c>i</c> is centered at <c>i * FrequencyResolution</c>.</param>
/// <param name="Magnitudes">Amplitude per bin from 0 Hz to Nyquist; a full-scale sine centered on a bin reads 1.</param>
public sealed record Spectrum(float FrequencyResolution, float[] Magnitudes);

/// <summary>
/// The spectrum module has no parameters yet.
/// </summary>
public sealed record SpectrumState;

/// <summary>
/// Passes audio through unchanged and keeps the latest samples (mixed to mono) for spectrum analysis.
/// </summary>
public class SpectrumModule : EffectModule, IStatefulModule<SpectrumState>, IModuleDataSource
{
    public const int FftSize = 4096;

    private readonly Complex[] _bins = new Complex[FftSize / 2 + 1];
    private readonly FftProcessor _fft = new(FftSize, FftWindowType.Hann);
    private readonly Lock _fftLock = new();
    private readonly float[] _history = new float[FftSize];
    private readonly float[] _window = new float[FftSize];
    private int _writePosition;

    /// <summary>
    /// Computes the spectrum of the last <see cref="FftSize"/> samples. Returns null until the module is in a graph.
    /// </summary>
    public object? GetData()
    {
        return GetSpectrum();
    }

    public SpectrumState GetState()
    {
        return new SpectrumState();
    }

    public void SetState(SpectrumState state)
    {
    }

    public Spectrum? GetSpectrum()
    {
        if (!IsAttached)
        {
            return null;
        }

        lock (_fftLock)
        {
            // The audio thread keeps writing; a torn window only affects this one snapshot.
            var position = Volatile.Read(ref _writePosition);
            _history.AsSpan(position).CopyTo(_window);
            _history.AsSpan(0, position).CopyTo(_window.AsSpan(FftSize - position));

            _fft.RealForward(_window, _bins);

            // RealForward normalizes by the FFT size; a Hann window halves the amplitude, and a real sine splits
            // its energy between two mirrored bins
            const float scale = 4f;
            var magnitudes = new float[_bins.Length];
            for (var i = 0; i < magnitudes.Length; i++)
            {
                magnitudes[i] = MathF.Sqrt(_bins[i].X * _bins[i].X + _bins[i].Y * _bins[i].Y) * scale;
            }

            return new Spectrum((float)Format.SampleRate / FftSize, magnitudes);
        }
    }

    protected override void Process(AudioBuffer buffer)
    {
        var channelGain = 1f / buffer.ChannelCount;
        var position = _writePosition;

        for (var i = 0; i < buffer.FrameCount; i++)
        {
            var sum = 0f;
            for (var c = 0; c < buffer.ChannelCount; c++)
            {
                sum += buffer.GetChannel(c)[i];
            }

            _history[position] = sum * channelGain;
            position = (position + 1) % FftSize;
        }

        Volatile.Write(ref _writePosition, position);
    }
}
