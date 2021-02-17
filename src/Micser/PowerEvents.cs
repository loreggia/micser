using System;

namespace Micser
{
    /// <summary>
    /// Static class providing events for power state changes.
    /// </summary>
    public static class PowerEvents
    {
        /// <summary>
        /// Fired when the computer suspends or resumes.
        /// </summary>
        public static event EventHandler<PowerStateEventArgs>? PowerStateChanged;

        /// <summary>
        /// Raises the <see cref="PowerStateChanged"/> event.
        /// </summary>
        public static void OnPowerStateChanged(PowerStateEventArgs e)
        {
            PowerStateChanged?.Invoke(null, e);
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="PowerEvents.PowerStateChanged"/> event.
    /// </summary>
    public class PowerStateEventArgs : EventArgs
    {
        /// <inheritdoc />
        public PowerStateEventArgs(bool isSuspending, bool isResuming)
        {
            IsSuspending = isSuspending;
            IsResuming = isResuming;
        }

        /// <summary>
        /// Gets a value that indicates whether the computer is resuming from a suspended state.
        /// </summary>
        public bool IsResuming { get; }

        /// <summary>
        /// Gets a value that indicates whether the computer is going into a suspended state.
        /// </summary>
        public bool IsSuspending { get; }
    }
}