using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Micser.Audio;

internal static partial class NativeMethods
{
    public const uint CreateWaitableTimerHighResolution = 0x00000002;
    public const uint Infinite = 0xFFFFFFFF;
    public const uint TimerAllAccess = 0x1F0003;

    [LibraryImport("avrt.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AvRevertMmThreadCharacteristics(nint avrtHandle);

    [LibraryImport("avrt.dll", EntryPoint = "AvSetMmThreadCharacteristicsW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint AvSetMmThreadCharacteristics(string taskName, ref uint taskIndex);

    [LibraryImport("kernel32.dll", EntryPoint = "CreateWaitableTimerExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial SafeWaitHandle CreateWaitableTimerEx(nint timerAttributes, string? timerName, uint flags, uint desiredAccess);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWaitableTimer(SafeWaitHandle timer, in long dueTime, int period, nint completionRoutine, nint argToCompletionRoutine, [MarshalAs(UnmanagedType.Bool)] bool resume);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial uint WaitForSingleObject(SafeWaitHandle handle, uint milliseconds);
}
