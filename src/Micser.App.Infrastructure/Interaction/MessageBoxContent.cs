using System.Windows;

namespace Micser.App.Infrastructure.Interaction
{
    /// <summary>
    /// Defines the content and layout of a message box.
    /// </summary>
    public class MessageBoxContent
    {
        /// <summary>
        /// Gets or sets the button layout to use.
        /// </summary>
        public MessageBoxButton Buttons { get; set; }

        /// <summary>
        /// Gets or sets the image type to show.
        /// </summary>
        public MessageBoxImage Image { get; set; }

        /// <summary>
        /// Gets or sets whether the dialog is shown as a modal window.
        /// </summary>
        public bool IsModal { get; set; }

        /// <summary>
        /// Gets or sets the message text.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the parent window instance to use when <see cref="IsModal"/> is <c>true</c>.
        /// </summary>
        public Window ParentWindow { get; set; }
    }
}