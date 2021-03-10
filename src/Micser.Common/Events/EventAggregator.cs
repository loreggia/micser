using System;
using System.Collections.Generic;

namespace Micser.Common.Events
{
    /// <inheritdoc />
    public class EventAggregator : IEventAggregator
    {
        private readonly List<Subscription> _subscriptions;

        /// <summary>
        /// Creates a new instance of the <see cref="EventAggregator"/> class.
        /// </summary>
        public EventAggregator()
        {
            _subscriptions = new List<Subscription>();
        }

        /// <inheritdoc />
        public void Publish<T>(T message)
        {
            var type = typeof(T);
            foreach (var subscription in _subscriptions)
            {
                if (subscription.CanHandle(type))
                {
                    subscription.Handle(message);
                }
            }
        }

        /// <inheritdoc />
        public IDisposable Subscribe<T>(Action<T> handler)
        {
            var subscription = new Subscription(typeof(T), handler);
            _subscriptions.Add(subscription);
            return new Disposable(() => _subscriptions.Remove(subscription));
        }

        private class Subscription
        {
            public Subscription(Type type, object handler)
            {
                Type = type;
                Handler = handler;
            }

            public object Handler { get; }
            public Type Type { get; }

            public bool CanHandle(Type type)
            {
                return Type.IsAssignableFrom(type);
            }

            public void Handle<T>(T evt)
            {
                ((Action<T>)Handler)(evt);
            }
        }
    }
}