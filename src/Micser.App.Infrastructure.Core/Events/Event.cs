using System;
using System.Collections.Generic;

namespace Micser.App.Infrastructure.Events
{
    public abstract class Event<TData>
    {
        private readonly List<IEventSubscription> _subscriptions;

        protected Event()
        {
            _subscriptions = new List<IEventSubscription>();
        }

        public virtual void Publish(TData data)
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Handler(data);
            }
        }

        public virtual IEventSubscription Subscribe(Action<TData> handler)
        {
            var subscription = new EventSubscription<TData>(handler);
            _subscriptions.Add(subscription);
            return subscription;
        }

        public void Unsubscribe(IEventSubscription subscription)
        {
            if (_subscriptions.Contains(subscription))
            {
                _subscriptions.Remove(subscription);
            }
        }
    }
}