using Micser.Common.Extensions;
using System;
using System.Linq.Expressions;
using System.Windows.Input;

namespace Micser.App.Infrastructure
{
    public static class CustomApplicationCommands
    {
        /// <summary>
        /// Closes the window or exits depending on settings.
        /// </summary>
        public static readonly RoutedUICommand Close;

        /// <summary>
        /// Performs a delete action in the current context.
        /// </summary>
        public static readonly RoutedUICommand Delete;

        /// <summary>
        /// Exits the application.
        /// </summary>
        public static readonly RoutedUICommand Exit;

        /// <summary>
        /// Performs an export action in the current context.
        /// </summary>
        public static readonly RoutedUICommand Export;

        /// <summary>
        /// Performs an import action in the current context.
        /// </summary>
        public static readonly RoutedUICommand Import;

        /// <summary>
        /// Refreshes the current view.
        /// </summary>
        public static readonly RoutedUICommand Refresh;

        /// <summary>
        /// Restores the minimized/hidden window.
        /// </summary>
        public static readonly RoutedUICommand Restore;

        /// <summary>
        /// Performs a save action in the current context.
        /// </summary>
        public static readonly RoutedUICommand Save;

        static CustomApplicationCommands()
        {
            Close = CreateCommand(() => Close);
            Exit = CreateCommand(() => Exit);
            Import = CreateCommand(() => Import);
            Export = CreateCommand(() => Export);
            Refresh = CreateCommand(() => Refresh);
            Restore = CreateCommand(() => Restore);
            Save = CreateCommand(() => Save);
            Delete = CreateCommand(() => Delete);
        }

        private static RoutedUICommand CreateCommand<T>(Expression<Func<T>> command)
        {
            var name = PropertyExtensions.GetName(command);
            return new RoutedUICommand(name, name, typeof(CustomApplicationCommands));
        }
    }
}