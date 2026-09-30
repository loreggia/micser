using NAudio.CoreAudioApi;

namespace Micser.Audio.Devices;

/// <summary>
/// Ring buffer sizing shared by capture and render streams.
/// </summary>
internal static class StreamBuffering
{
    /// <summary>
    /// Above <c>target * MaxFillFactor</c> a stream resynchronizes instead of relying on the drift correction.
    /// </summary>
    public const double MaxFillFactor = 3;

    /// <summary>
    /// Below <c>target * MinFillFactor</c> a render stream tops up with silence instead of relying on the drift correction.
    /// </summary>
    public const double MinFillFactor = 0.5;

    /// <summary>
    /// The fill level in device frames: one device period, because devices deliver and consume whole periods, plus half
    /// an engine block for timer jitter.
    /// </summary>
    public static double GetTargetFill(MMDevice device, ProcessingFormat format, int deviceSampleRate)
    {
        TimeSpan devicePeriod;
        using (var client = device.CreateAudioClient())
        {
            // 100 ns units
            devicePeriod = TimeSpan.FromTicks(client.DefaultDevicePeriod);
        }

        return (devicePeriod.TotalSeconds + format.BlockDuration.TotalSeconds / 2) * deviceSampleRate;
    }
}
