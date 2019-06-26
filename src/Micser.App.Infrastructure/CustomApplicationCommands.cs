using Micser.Common.Extensions;
using System;
using System.Linq.Expressions;
using System.Windows.Input;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Contains custom <see cref="RoutedUICommand"/>s.
    /// </summary>
    public static class CustomApplicationCommands
    {
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

        static CustomApplicationCommands()
        {
            Exit = CreateCommand(() => Exit);
            Import = CreateCommand(() => Import);
            Export = CreateCommand(() => Export);
            Refresh = CreateCommand(() => Refresh);
            Restore = CreateCommand(() => Restore);
            Delete = CreateCommand(() => Delete);
        }

        /// <summary>
        /// Helper function to create a <see cref="RoutedUICommand"/>.
        /// </summary>
        private static RoutedUICommand CreateCommand<T>(Expression<Func<T>> command)
        {
            var name = PropertyExtensions.GetName(command);
            return new RoutedUICommand(name, name, typeof(CustomApplicationCommands));
        }
    }
}