using System.Collections.Generic;

namespace Micser.App.Infrastructure.Events
{
    public abstract class EventBase
    {
        private readonly List<IEventSubscription> _subscriptions;

        protected EventBase()
        {
            _subscriptions = new List<IEventSubscription>();
        }

        public void Unsubscribe(IEventSubscription subscription)
        {
            if (_subscriptions.Contains(subscription))
            {
                _subscriptions.Remove(subscription);
            }
        }

        protected void PublishInternal(params object[] args)
        {
            foreach (var subscription in _subscriptions)
            {
                var executionStrategy = subscription.GetExecutionStrategy();
                executionStrategy?.Invoke(args);
            }
        }

        protected void SubscribeInternal(IEventSubscription subscription)
        {
            _subscriptions.Add(subscription);
        }
    }
}