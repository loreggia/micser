using Micser.Plugins.Main.Dsp;

namespace Micser.Plugins.Main.Tests.Dsp;

/// <summary>
/// Synthetic signals at 48 kHz and spectra to check them.
/// </summary>
internal static class TestSignals
{
    public const float SampleRate = 48000f;
    private const int SpectrumSize = 8192;

    public static float DistanceToHarmonic(float frequency, float f0)
    {
        return Math.Abs(frequency - (f0 * MathF.Round(frequency / f0)));
    }

    /// <summary>
    /// The strongest magnitude within two bins of <paramref name="frequency"/> in <see cref="Spectrum"/>.
    /// </summary>
    public static float MagnitudeAt(float[] spectrum, float frequency)
    {
        var bin = (int)MathF.Round(frequency * SpectrumSize / SampleRate);
        var magnitude = 0f;
        for (var k = Math.Max(0, bin - 2); k <= Math.Min(spectrum.Length - 1, bin + 2); k++)
        {
            magnitude = Math.Max(magnitude, spectrum[k]);
        }

        return magnitude;
    }

    /// <summary>
    /// The frequency of the strongest component in <see cref="Spectrum"/>, above DC.
    /// </summary>
    public static float PeakFrequency(float[] spectrum)
    {
        var peak = 1;
        for (var k = 2; k < spectrum.Length; k++)
        {
            if (spectrum[k] > spectrum[peak])
            {
                peak = k;
            }
        }

        return peak * SampleRate / SpectrumSize;
    }

    public static Func<int, float> Sine(float frequency, float amplitude = 0.5f)
    {
        return n => amplitude * MathF.Sin(2 * MathF.PI * frequency * n / SampleRate);
    }

    /// <summary>
    /// The magnitudes of the last 8192 samples (Hann window), one per bin of 48000 / 8192 Hz up to half the sample rate.
    /// </summary>
    public static float[] Spectrum(float[] signal)
    {
        var window = new float[SpectrumSize];
        HannWindow.Fill(window);
        var data = new float[2 * SpectrumSize];
        for (var n = 0; n < SpectrumSize; n++)
        {
            data[2 * n] = signal[signal.Length - SpectrumSize + n] * window[n];
        }

        new Fft(SpectrumSize).Transform(data, SpectrumSize, inverse: false);
        var magnitudes = new float[SpectrumSize / 2];
        for (var k = 0; k < magnitudes.Length; k++)
        {
            magnitudes[k] = MathF.Sqrt((data[2 * k] * data[2 * k]) + (data[(2 * k) + 1] * data[(2 * k) + 1]));
        }

        return magnitudes;
    }

    /// <summary>
    /// Harmonics of <paramref name="f0"/> up to 8 kHz, shaped by a single formant around <paramref name="formant"/>.
    /// </summary>
    public static Func<int, float> Vowel(float f0, float formant)
    {
        return n =>
        {
            var sample = 0.0;
            for (var h = 1; h * f0 < 8000f; h++)
            {
                var frequency = h * f0;
                var distance = (frequency - formant) / 300.0;
                var amplitude = (1.0 / (1.0 + (distance * distance))) + 0.02;
                sample += 0.05 * amplitude * Math.Sin(2 * Math.PI * frequency * n / SampleRate);
            }

            return (float)sample;
        };
    }
}
