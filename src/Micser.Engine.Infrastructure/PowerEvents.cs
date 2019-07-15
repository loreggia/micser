using System;

namespace Micser.Engine.Infrastructure
{
    public delegate void PowerStateEventHandler(object sender, PowerStateEventArgs e);

    public static class PowerEvents
    {
        public static event PowerStateEventHandler PowerStateChanged;

        public static void OnPowerStateChanged(PowerStateEventArgs e)
        {
            PowerStateChanged?.Invoke(null, e);
        }
    }

    public class PowerStateEventArgs : EventArgs
    {
        public PowerStateEventArgs(bool isSuspending, bool isResuming)
        {
            IsSuspending = isSuspending;
            IsResuming = isResuming;
        }

        public bool IsResuming { get; }
        public bool IsSuspending { get; }
    }
}