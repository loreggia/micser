using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Tmds.DBus.Protocol;

namespace Micser.Audio;

/// <summary>
/// What <see cref="RealtimeKit.Acquire"/> got: a realtime <see cref="Priority"/>, otherwise a raised <see cref="Nice"/> level, otherwise
/// neither, with the <see cref="Error"/>s.
/// </summary>
internal sealed record RealtimeKitResult(int? Priority, int? Nice, string? Error);

/// <summary>
/// RTKit (<c>org.freedesktop.RealtimeKit1</c> on the system bus), which gives threads a realtime priority for users without
/// <c>RLIMIT_RTPRIO</c>, as it does for PipeWire's threads. It requires the process's <c>RLIMIT_RTTIME</c> to be at most its
/// <c>RTTimeUSecMax</c>: the CPU time a realtime thread may use without a blocking call, after which the kernel kills the process.
/// </summary>
[SupportedOSPlatform("linux")]
internal static class RealtimeKit
{
    public const string ServiceName = "org.freedesktop.RealtimeKit1";
    public const string ObjectPath = "/org/freedesktop/RealtimeKit1";

    // PipeWire's own nice level for its main thread
    private const int HighPriorityNice = -11;

    /// <summary>
    /// Makes the thread a realtime thread with at most <paramref name="priority"/>, but below RTKit's maximum, which PipeWire's threads
    /// get; if RTKit refuses, raises the thread's nice level instead. Sets the process's <c>RLIMIT_RTTIME</c> first. Blocks up to
    /// <paramref name="timeout"/>.
    /// </summary>
    /// <param name="busAddress">The system bus (<see cref="DBusAddress.System"/>), or another one in tests.</param>
    /// <param name="threadId">The thread's kernel id (<c>gettid</c>).</param>
    public static RealtimeKitResult Acquire(string busAddress, long threadId, int priority, TimeSpan timeout)
    {
        var acquire = Task.Run(() => AcquireAsync(busAddress, threadId, priority));
        try
        {
            return acquire.Wait(timeout)
                ? acquire.Result
                : new RealtimeKitResult(null, null, $"no answer within {timeout.TotalSeconds:0} s");
        }
        catch (AggregateException ex)
        {
            // no system bus, no RTKit, or an unexpected reply; the engine runs without a raised priority then
            return new RealtimeKitResult(null, null, ex.InnerException?.Message ?? ex.Message);
        }
    }

    private static async Task<RealtimeKitResult> AcquireAsync(string busAddress, long threadId, int priority)
    {
        using var connection = new DBusConnection(busAddress);
        await connection.ConnectAsync();
        var maxPriority = (await GetPropertyAsync(connection, "MaxRealtimePriority")).GetInt32();
        var minNice = (await GetPropertyAsync(connection, "MinNiceLevel")).GetInt32();
        var maxRealtimeTime = (await GetPropertyAsync(connection, "RTTimeUSecMax")).GetInt64();
        LimitRealtimeTime((ulong)maxRealtimeTime);

        var granted = Math.Clamp(priority, 1, Math.Max(1, maxPriority - 1));
        string realtimeError;
        try
        {
            await connection.CallMethodAsync(CreateMakeRealtime(connection, threadId, granted));
            return new RealtimeKitResult(granted, null, null);
        }
        catch (DBusErrorReplyException ex)
        {
            realtimeError = ex.Message;
        }

        var nice = Math.Max(minNice, HighPriorityNice);
        try
        {
            await connection.CallMethodAsync(CreateMakeHighPriority(connection, threadId, nice));
            return new RealtimeKitResult(null, nice, realtimeError);
        }
        catch (DBusErrorReplyException ex)
        {
            return new RealtimeKitResult(null, null, $"{realtimeError}; {ex.Message}");
        }
    }

    private static MessageBuffer CreateGetProperty(DBusConnection connection, string name)
    {
        using var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            ServiceName,
            ObjectPath,
            "org.freedesktop.DBus.Properties",
            "Get",
            "ss",
            MessageFlags.None
        );
        writer.WriteString(ServiceName);
        writer.WriteString(name);
        return writer.CreateMessage();
    }

    private static MessageBuffer CreateMakeHighPriority(DBusConnection connection, long threadId, int nice)
    {
        using var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            ServiceName,
            ObjectPath,
            ServiceName,
            "MakeThreadHighPriority",
            "ti",
            MessageFlags.None
        );
        writer.WriteUInt64((ulong)threadId);
        writer.WriteInt32(nice);
        return writer.CreateMessage();
    }

    private static MessageBuffer CreateMakeRealtime(DBusConnection connection, long threadId, int priority)
    {
        using var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            ServiceName,
            ObjectPath,
            ServiceName,
            "MakeThreadRealtime",
            "tu",
            MessageFlags.None
        );
        writer.WriteUInt64((ulong)threadId);
        writer.WriteUInt32((uint)priority);
        return writer.CreateMessage();
    }

    private static Task<VariantValue> GetPropertyAsync(DBusConnection connection, string name)
    {
        return connection.CallMethodAsync(
            CreateGetProperty(connection, name),
            static (message, _) => message.GetBodyReader().ReadVariantValue(),
            null
        );
    }

    /// <summary>
    /// Lowers the process's <c>RLIMIT_RTTIME</c> to RTKit's maximum (both limits, as PipeWire does): the audio thread blocks for every
    /// block, so only a hung graph would reach it, and the shell then starts a new engine.
    /// </summary>
    private static void LimitRealtimeTime(ulong maximum)
    {
        if (LinuxNativeMethods.GetRLimit(LinuxNativeMethods.RlimitRtTime, out var current) != 0)
        {
            throw new InvalidOperationException($"getrlimit failed (error {Marshal.GetLastPInvokeError()}).");
        }

        var limit = Math.Min(current.Maximum, maximum);
        if (current.Current <= limit && current.Maximum <= limit)
        {
            return;
        }

        var lowered = new LinuxNativeMethods.RLimit { Current = Math.Min(current.Current, limit), Maximum = limit };
        if (LinuxNativeMethods.SetRLimit(LinuxNativeMethods.RlimitRtTime, lowered) != 0)
        {
            throw new InvalidOperationException($"setrlimit failed (error {Marshal.GetLastPInvokeError()}).");
        }
    }
}
