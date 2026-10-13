using Micser.Audio;

namespace Micser.Plugins.Main.Dsp;

/// <summary>
/// Per-frame helpers for stereo-linked dynamics: the peak of all channels and one gain for all channels per frame.
/// </summary>
internal static class FrameGains
{
    /// <summary>
    /// Writes the highest absolute sample of all channels of each frame to <paramref name="peaks"/>; zeros without channels.
    /// </summary>
    public static void ComputePeaks(this AudioBuffer buffer, Span<float> peaks)
    {
        peaks.Clear();
        for (var c = 0; c < buffer.ChannelCount; c++)
        {
            var channel = buffer.GetChannel(c);
            for (var i = 0; i < channel.Length; i++)
            {
                peaks[i] = Math.Max(peaks[i], Math.Abs(channel[i]));
            }
        }
    }

    /// <summary>
    /// Multiplies each frame of all channels by its gain.
    /// </summary>
    public static void ApplyGains(this AudioBuffer buffer, ReadOnlySpan<float> gains)
    {
        for (var c = 0; c < buffer.ChannelCount; c++)
        {
            var channel = buffer.GetChannel(c);
            for (var i = 0; i < channel.Length; i++)
            {
                channel[i] *= gains[i];
            }
        }
    }
}
