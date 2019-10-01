using System;

namespace Micser.App.Infrastructure.Events
{
    public class EventSubscription<T> : IEventSubscription
    {
        public EventSubscription(Action<T> handler)
        {
            Handler = x => handler((T)x);
        }

        public Action<object> Handler { get; }
    }
}