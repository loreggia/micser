using System.Windows.Input;

namespace Micser.Infrastructure
{
    public static class CustomApplicationCommands
    {
        /// <summary>
        /// Closes the window or exits depending on settings.
        /// </summary>
        public static readonly RoutedUICommand Close;

        /// <summary>
        /// Exits the application.
        /// </summary>
        public static readonly RoutedUICommand Exit;

        /// <summary>
        /// Restores the minimized/hidden window.
        /// </summary>
        public static readonly RoutedUICommand Restore;

        static CustomApplicationCommands()
        {
            Close = new RoutedUICommand(nameof(Close), nameof(Close), typeof(CustomApplicationCommands));
            Exit = new RoutedUICommand(nameof(Exit), nameof(Exit), typeof(CustomApplicationCommands));
            Restore = new RoutedUICommand(nameof(Restore), nameof(Restore), typeof(CustomApplicationCommands));
        }
    }
}