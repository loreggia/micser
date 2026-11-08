namespace Micser.Plugins.Main.Dsp;

internal static class HannWindow
{
    /// <summary>
    /// Fills <paramref name="window"/> with a periodic Hann window of its length, as STFT frames use it.
    /// </summary>
    public static void Fill(Span<float> window)
    {
        for (var k = 0; k < window.Length; k++)
        {
            window[k] = (float)((-.5 * Math.Cos(2.0 * Math.PI * k / window.Length)) + .5);
        }
    }
}
