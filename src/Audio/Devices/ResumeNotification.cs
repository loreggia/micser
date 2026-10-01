using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Micser.Audio.Devices;

/// <summary>
/// Calls back when the system resumed from sleep or hibernation. Uses a power callback, so no window or message loop is needed.
/// </summary>
internal sealed unsafe class ResumeNotification : IDisposable
{
    private readonly Action _onResumed;
    private readonly nint _parameters;
    private readonly nint _registration;
    private GCHandle _self;

    /// <param name="onResumed">Called on a thread pool thread.</param>
    /// <exception cref="Win32Exception">The registration failed.</exception>
    public ResumeNotification(Action onResumed)
    {
        _onResumed = onResumed;
        _self = GCHandle.Alloc(this);

        // the system may keep the pointer, so the parameters live in unmanaged memory until Dispose
        _parameters = Marshal.AllocHGlobal(sizeof(NativeMethods.DeviceNotifySubscribeParameters));
        *(NativeMethods.DeviceNotifySubscribeParameters*)_parameters = new NativeMethods.DeviceNotifySubscribeParameters
        {
            Callback = (nint)(delegate* unmanaged<nint, uint, nint, uint>)&OnPowerEvent,
            Context = GCHandle.ToIntPtr(_self),
        };

        var result = NativeMethods.PowerRegisterSuspendResumeNotification(NativeMethods.DeviceNotifyCallback, _parameters, out _registration);
        if (result != 0)
        {
            Marshal.FreeHGlobal(_parameters);
            _self.Free();
            throw new Win32Exception((int)result);
        }
    }

    public void Dispose()
    {
        if (!_self.IsAllocated)
        {
            return;
        }

        NativeMethods.PowerUnregisterSuspendResumeNotification(_registration);
        Marshal.FreeHGlobal(_parameters);
        _self.Free();
    }

    [UnmanagedCallersOnly]
    private static uint OnPowerEvent(nint context, uint type, nint setting)
    {
        // sent on every resume, also without user input; the callback must return quickly
        if (type == NativeMethods.PbtApmResumeAutomatic && GCHandle.FromIntPtr(context).Target is ResumeNotification notification)
        {
            ThreadPool.QueueUserWorkItem(_ => notification._onResumed());
        }

        return 0;
    }
}
