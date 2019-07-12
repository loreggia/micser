using Prism.Events;
using System;

namespace Micser.App.Infrastructure.Interaction
{
    public enum MessageBoxType
    {
        Information,
        Warning,
        Error,
        Question
    }

    public class MessageBoxEvent : PubSubEvent<MessageBoxEventArgs>
    {
    }

    public class MessageBoxEventArgs
    {
        public MessageBoxEventArgs()
        {
            IsModal = true;
            Type = MessageBoxType.Information;
        }

        public Action<bool> Callback { get; set; }
        public bool IsModal { get; set; }
        public string Message { get; set; }
        public string Title { get; set; }
        public MessageBoxType Type { get; set; }
    }
}