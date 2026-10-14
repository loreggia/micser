using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Dsp;

/// <summary>
/// A second-order IIR filter (transposed direct form II) with the coefficients of the Audio EQ Cookbook (R. Bristow-Johnson). Shelves
/// take a Q like the other types, as in Equalizer APO's LSC/HSC filters.
/// </summary>
internal sealed class Biquad
{
    private double _a1;
    private double _a2;
    private double _b0 = 1;
    private double _b1;
    private double _b2;
    private double _z1;
    private double _z2;

    public void Process(Span<float> samples)
    {
        var (b0, b1, b2, a1, a2) = (_b0, _b1, _b2, _a1, _a2);
        var (z1, z2) = (_z1, _z2);
        for (var i = 0; i < samples.Length; i++)
        {
            double x = samples[i];
            var y = (b0 * x) + z1;
            z1 = (b1 * x) - (a1 * y) + z2;
            z2 = (b2 * x) - (a2 * y);
            samples[i] = (float)y;
        }

        // keeps denormals out of the state after silence
        (_z1, _z2) = (Math.Abs(z1) < 1e-30 ? 0 : z1, Math.Abs(z2) < 1e-30 ? 0 : z2);
    }

    /// <summary>
    /// Sets the coefficients and keeps the state. The frequency is limited to below the Nyquist frequency.
    /// </summary>
    public void Set(EqualizerBandType type, int sampleRate, float frequency, float q, float gainDecibels)
    {
        var w0 = 2 * Math.PI * Math.Min(frequency, sampleRate * 0.45) / sampleRate;
        var cos = Math.Cos(w0);
        var alpha = Math.Sin(w0) / (2 * Math.Max(q, 0.01));
        var a = Math.Pow(10, gainDecibels / 40.0);
        var shelf = 2 * Math.Sqrt(a) * alpha;

        var (b0, b1, b2, a0, a1, a2) = type switch
        {
            EqualizerBandType.LowShelf => (
                a * (a + 1 - ((a - 1) * cos) + shelf),
                2 * a * (a - 1 - ((a + 1) * cos)),
                a * (a + 1 - ((a - 1) * cos) - shelf),
                a + 1 + ((a - 1) * cos) + shelf,
                -2 * (a - 1 + ((a + 1) * cos)),
                a + 1 + ((a - 1) * cos) - shelf
            ),
            EqualizerBandType.HighShelf => (
                a * (a + 1 + ((a - 1) * cos) + shelf),
                -2 * a * (a - 1 + ((a + 1) * cos)),
                a * (a + 1 + ((a - 1) * cos) - shelf),
                a + 1 - ((a - 1) * cos) + shelf,
                2 * (a - 1 - ((a + 1) * cos)),
                a + 1 - ((a - 1) * cos) - shelf
            ),
            EqualizerBandType.LowPass => ((1 - cos) / 2, 1 - cos, (1 - cos) / 2, 1 + alpha, -2 * cos, 1 - alpha),
            EqualizerBandType.HighPass => ((1 + cos) / 2, -(1 + cos), (1 + cos) / 2, 1 + alpha, -2 * cos, 1 - alpha),
            _ => (1 + (alpha * a), -2 * cos, 1 - (alpha * a), 1 + (alpha / a), -2 * cos, 1 - (alpha / a)),
        };

        (_b0, _b1, _b2, _a1, _a2) = (b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0);
    }
}
