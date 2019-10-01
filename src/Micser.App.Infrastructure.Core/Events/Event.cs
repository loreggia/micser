using System;

namespace Micser.App.Infrastructure.Events
{
    public abstract class Event : EventBase
    {
        public virtual void Publish()
        {
            PublishInternal(null);
        }

        public virtual IEventSubscription Subscribe(Action handler)
        {
            var subscription = new EventSubscription(handler);
            SubscribeInternal(subscription);
            return subscription;
        }
    }

    public abstract class Event<T> : EventBase
    {
        public virtual void Publish(T data)
        {
            PublishInternal(data);
        }

        public virtual IEventSubscription Subscribe(Action<T> handler)
        {
            var subscription = new EventSubscription<T>(handler);
            SubscribeInternal(subscription);
            return subscription;
        }
    }
}