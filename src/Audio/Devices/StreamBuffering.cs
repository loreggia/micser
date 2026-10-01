using NAudio.CoreAudioApi;

namespace Micser.Audio.Devices;

/// <summary>
/// Ring buffer sizing and stall detection shared by capture and render streams.
/// </summary>
internal static class StreamBuffering
{
    /// <summary>
    /// The engine side of a stream is idle when it hasn't read or written for this long, e.g. while audio is switched off. The device's
    /// empty reads or full writes then aren't dropouts.
    /// </summary>
    public const long IdleMilliseconds = 100;

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
    /// Dropouts within this time after the target grew count as the same incident.
    /// </summary>
    private const double HoldOffSeconds = 1;

    /// <summary>
    /// The highest buffer target.
    /// </summary>
    private const double MaxTargetSeconds = 0.2;

    /// <summary>
    /// Time without dropouts before the target shrinks again.
    /// </summary>
    private const double StableSeconds = 600;

    /// <summary>
    /// The buffer target of a capture stream, in device frames. A read takes a whole engine block while the device delivers whole periods
    /// at any phase, so the fill before a read must cover a block plus half a period; at least one period, plus half a block for timer
    /// jitter. It grows by half a period per dropout.
    /// </summary>
    public static AdaptiveTarget CreateCaptureTarget(MMDevice device, ProcessingFormat format, int deviceSampleRate)
    {
        var period = GetDevicePeriod(device);
        var block = format.BlockDuration.TotalSeconds;
        return CreateTarget(Math.Max(period, block + period / 2) + block / 2, period, format, deviceSampleRate);
    }

    /// <summary>
    /// The buffer target of a render stream, in device frames: one device period, because devices take whole periods, plus half an
    /// engine block for timer jitter. It grows by half a period per dropout.
    /// </summary>
    public static AdaptiveTarget CreateRenderTarget(MMDevice device, ProcessingFormat format, int deviceSampleRate)
    {
        var period = GetDevicePeriod(device);
        return CreateTarget(period + format.BlockDuration.TotalSeconds / 2, period, format, deviceSampleRate);
    }

    /// <summary>
    /// Whether the engine side is idle; <paramref name="lastActivity"/> is a <see cref="Environment.TickCount64"/> value, 0 if there was none.
    /// </summary>
    public static bool IsIdle(long lastActivity, long now)
    {
        return now - lastActivity > IdleMilliseconds;
    }

    /// <summary>
    /// Whether a stream opened at <paramref name="openedAt"/> is stalled. Times are <see cref="Environment.TickCount64"/> values;
    /// <paramref name="lastActivity"/> is 0 before the device's first callback.
    /// </summary>
    public static bool IsStalled(long openedAt, long lastActivity, long now)
    {
        return lastActivity == 0 ? now - openedAt > StartTimeoutMilliseconds : now - lastActivity > StallTimeoutMilliseconds;
    }

    private static AdaptiveTarget CreateTarget(double minimumSeconds, double periodSeconds, ProcessingFormat format, int deviceSampleRate)
    {
        var blocksPerSecond = 1 / format.BlockDuration.TotalSeconds;
        return new AdaptiveTarget(
            minimumSeconds * deviceSampleRate,
            periodSeconds / 2 * deviceSampleRate,
            MaxTargetSeconds * deviceSampleRate,
            (long)(HoldOffSeconds * blocksPerSecond),
            (long)(StableSeconds * blocksPerSecond));
    }

    private static double GetDevicePeriod(MMDevice device)
    {
        using var client = device.CreateAudioClient();

        // 100 ns units
        return TimeSpan.FromTicks(client.DefaultDevicePeriod).TotalSeconds;
    }
}
