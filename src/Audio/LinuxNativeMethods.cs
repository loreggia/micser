using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Micser.Audio;

[SupportedOSPlatform("linux")]
internal static partial class LinuxNativeMethods
{
    public const int ClockMonotonic = 1;
    public const int SchedFifo = 1;
    public const int SchedResetOnFork = 0x40000000;

    [LibraryImport("libc", EntryPoint = "clock_nanosleep")]
    public static partial int ClockNanosleep(int clockId, int flags, in TimeSpec request, nint remaining);

    /// <summary>
    /// With <paramref name="pid"/> 0, Linux applies it to the calling thread only.
    /// </summary>
    [LibraryImport("libc", EntryPoint = "sched_setscheduler", SetLastError = true)]
    public static partial int SchedSetScheduler(int pid, int policy, in SchedParam parameters);

    [StructLayout(LayoutKind.Sequential)]
    public struct SchedParam
    {
        public int Priority;
    }

    /// <summary>
    /// <c>struct timespec</c> on 64-bit Linux.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TimeSpec
    {
        public long Seconds;
        public long Nanoseconds;
    }
}
