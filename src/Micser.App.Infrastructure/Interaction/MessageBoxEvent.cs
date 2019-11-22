using Prism.Events;
using System;

namespace Micser.App.Infrastructure.Interaction
{
    /// <summary>
    /// Defines types of a <see cref="MessageBoxEvent"/>.
    /// </summary>
    public enum MessageBoxType
    {
        /// <summary>
        /// An information dialog with an OK button.
        /// </summary>
        Information,

        /// <summary>
        /// A warning dialog with an OK button.
        /// </summary>
        Warning,

        /// <summary>
        /// An error dialog with an OK button.
        /// </summary>
        Error,

        /// <summary>
        /// A question dialog with yes and no buttons.
        /// </summary>
        Question
    }

    /// <summary>
    /// A <see cref="PubSubEvent{TPayload}"/> for displaying a message box.
    /// </summary>
    public class MessageBoxEvent : PubSubEvent<MessageBoxEventArgs>
    {
    }

    /// <summary>
    /// Defines the content and layout of a <see cref="MessageBoxEvent"/>.
    /// </summary>
    public class MessageBoxEventArgs
    {
        /// <inheritdoc />
        public MessageBoxEventArgs()
        {
            IsModal = true;
            Type = MessageBoxType.Information;
        }

        /// <summary>
        /// Gets or sets a callback function that is called when the message box is closed.
        /// </summary>
        public Action<bool> Callback { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message box is shown as a modal window.
        /// </summary>
        public bool IsModal { get; set; }

        /// <summary>
        /// Gets or sets the message text.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the title text.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the type of the message box. This sets the corresponding icon and button text/layout.
        /// </summary>
        public MessageBoxType Type { get; set; }
    }
}