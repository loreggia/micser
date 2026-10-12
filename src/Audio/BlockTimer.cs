using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;

namespace Micser.Audio;

/// <summary>
/// Waits for the engine's block deadlines and raises the audio thread's scheduling priority while it exists. Created and used on the
/// audio thread only.
/// </summary>
internal abstract class BlockTimer : IDisposable
{
    /// <summary>
    /// The timer of the current platform; also gives the calling thread a realtime priority where the platform allows it.
    /// </summary>
    public static BlockTimer Create(ILogger logger)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsBlockTimer(logger);
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxBlockTimer(logger);
        }

        return new SleepBlockTimer();
    }

    public virtual void Dispose() { }

    /// <summary>
    /// Returns at <paramref name="deadline"/> (a <see cref="Stopwatch"/> timestamp) or right away if it has passed.
    /// </summary>
    public void WaitUntil(long deadline)
    {
        var remaining = deadline - Stopwatch.GetTimestamp();
        if (remaining > 0)
        {
            Wait(TimeSpan.FromSeconds((double)remaining / Stopwatch.Frequency));
        }
    }

    protected abstract void Wait(TimeSpan duration);

    private sealed class SleepBlockTimer : BlockTimer
    {
        protected override void Wait(TimeSpan duration)
        {
            Thread.Sleep(duration);
        }
    }

    /// <summary>
    /// A high resolution waitable timer, with the thread registered with MMCSS as "Pro Audio".
    /// </summary>
    [SupportedOSPlatform("windows")]
    private sealed class WindowsBlockTimer : BlockTimer
    {
        private readonly nint _mmcssHandle;
        private readonly SafeWaitHandle _timer;

        public WindowsBlockTimer(ILogger logger)
        {
            uint taskIndex = 0;
            _mmcssHandle = NativeMethods.AvSetMmThreadCharacteristics("Pro Audio", ref taskIndex);
            if (_mmcssHandle == 0)
            {
                logger.LogWarning("Could not register the audio thread with MMCSS.");
            }

            _timer = NativeMethods.CreateWaitableTimerEx(
                0,
                null,
                NativeMethods.CreateWaitableTimerHighResolution,
                NativeMethods.TimerAllAccess
            );
            if (_timer.IsInvalid)
            {
                logger.LogWarning("High resolution timers are not available; block timing will be less precise.");
            }
        }

        public override void Dispose()
        {
            _timer.Dispose();
            if (_mmcssHandle != 0)
            {
                NativeMethods.AvRevertMmThreadCharacteristics(_mmcssHandle);
            }
        }

        protected override void Wait(TimeSpan duration)
        {
            if (_timer.IsInvalid)
            {
                Thread.Sleep(duration);
                return;
            }

            // negative due time = relative, in 100 ns units
            if (NativeMethods.SetWaitableTimer(_timer, -duration.Ticks, 0, 0, 0, false))
            {
                NativeMethods.WaitForSingleObject(_timer, NativeMethods.Infinite);
            }
        }
    }

    /// <summary>
    /// <c>clock_nanosleep</c> on the monotonic clock, with the thread on <c>SCHED_FIFO</c> if the user may use realtime priorities
    /// (<c>RLIMIT_RTPRIO</c>, e.g. through PipeWire's or JACK's limits for the audio group), otherwise through RTKit, as PipeWire's
    /// threads.
    /// </summary>
    [SupportedOSPlatform("linux")]
    private sealed class LinuxBlockTimer : BlockTimer
    {
        // below the realtime threads of PipeWire, which feed the device streams
        private const int RealtimePriority = 20;

        private static readonly TimeSpan RealtimeKitTimeout = TimeSpan.FromSeconds(2);

        public LinuxBlockTimer(ILogger logger)
        {
            var parameters = new LinuxNativeMethods.SchedParam { Priority = RealtimePriority };
            if (
                LinuxNativeMethods.SchedSetScheduler(
                    0,
                    LinuxNativeMethods.SchedFifo | LinuxNativeMethods.SchedResetOnFork,
                    parameters
                ) == 0
            )
            {
                return;
            }

            var error = Marshal.GetLastPInvokeError();
            if (error != LinuxNativeMethods.ErrorNotPermitted)
            {
                logger.LogWarning(
                    "Could not give the audio thread a realtime priority (error {Error}); block timing may suffer under load.",
                    error
                );
                return;
            }

            var result = RealtimeKit.Acquire(
                DBusAddress.System!,
                LinuxNativeMethods.GetTid(),
                RealtimePriority,
                RealtimeKitTimeout
            );
            if (result.Priority is { } priority)
            {
                logger.LogInformation("The audio thread has realtime priority {Priority} through RTKit.", priority);
            }
            else if (result.Nice is { } nice)
            {
                logger.LogWarning(
                    "RTKit gave the audio thread a raised priority (nice {Nice}), but no realtime priority ({Error}); block timing may suffer under load.",
                    nice,
                    result.Error
                );
            }
            else
            {
                logger.LogWarning(
                    "Could not give the audio thread a realtime priority, neither directly nor through RTKit ({Error}); block timing may suffer under load.",
                    result.Error
                );
            }
        }

        protected override void Wait(TimeSpan duration)
        {
            var request = new LinuxNativeMethods.TimeSpec
            {
                Seconds = duration.Ticks / TimeSpan.TicksPerSecond,
                Nanoseconds = duration.Ticks % TimeSpan.TicksPerSecond * 100,
            };

            // returns the error instead of setting errno; an interrupted sleep ends early, and the engine waits again for its next block
            LinuxNativeMethods.ClockNanosleep(LinuxNativeMethods.ClockMonotonic, 0, request, 0);
        }
    }
}
