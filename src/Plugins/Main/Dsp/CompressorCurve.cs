namespace Micser.Plugins.Main.Dsp;

/// <summary>
/// Static input/output level curves of a compressor, in dB.
/// </summary>
internal static class CompressorCurve
{
    /// <summary>
    /// Reduces levels above the threshold by <paramref name="slope"/> (1 / ratio), with a soft knee of <paramref name="knee"/> dB.
    /// </summary>
    public static float Downward(float level, float slope, float threshold, float knee)
    {
        if (knee <= 0f)
        {
            return level < threshold ? level : threshold + slope * (level - threshold);
        }

        if (level - threshold < -knee / 2f)
        {
            return level;
        }

        if (Math.Abs(level - threshold) <= knee / 2f)
        {
            var a = level - threshold + knee / 2f;
            return level + (slope - 1f) * a * a / (2f * knee);
        }

        return threshold + slope * (level - threshold);
    }

    /// <summary>
    /// Raises levels below the threshold towards it by <paramref name="slope"/> (1 / ratio), with a soft knee of <paramref name="knee"/> dB.
    /// </summary>
    public static float Upward(float level, float slope, float threshold, float knee)
    {
        if (knee <= 0f)
        {
            return level > threshold ? level : threshold + slope * (level - threshold);
        }

        if (level - threshold > knee / 2f)
        {
            return level;
        }

        if (Math.Abs(level - threshold) <= knee / 2f)
        {
            var a = -level + threshold + knee / 2f;
            return level - (slope - 1f) * a * a / (2f * knee);
        }

        return threshold + slope * (level - threshold);
    }
}
