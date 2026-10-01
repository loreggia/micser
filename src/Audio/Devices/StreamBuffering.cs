using NAudio.CoreAudioApi;

namespace Micser.Audio.Devices;

/// <summary>
/// Ring buffer sizing and stall detection shared by capture and render streams.
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
    /// A stream whose device hasn't delivered or taken data for this long is stalled, e.g. after the system resumed from sleep.
    /// </summary>
    public const long StallTimeoutMilliseconds = 2000;

    /// <summary>
    /// How long a device may take to deliver or take its first data, e.g. a Bluetooth device that is still connecting.
    /// </summary>
    public const long StartTimeoutMilliseconds = 10000;

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

    /// <summary>
    /// Whether a stream opened at <paramref name="openedAt"/> is stalled. Times are <see cref="Environment.TickCount64"/> values;
    /// <paramref name="lastActivity"/> is 0 before the device's first callback.
    /// </summary>
    public static bool IsStalled(long openedAt, long lastActivity, long now)
    {
        return lastActivity == 0 ? now - openedAt > StartTimeoutMilliseconds : now - lastActivity > StallTimeoutMilliseconds;
    }
}
