using System;

namespace Micser.Common.Events
{
    /// <summary>
    /// A pub-sub event aggregator.
    /// </summary>
    public interface IEventAggregator
    {
        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <typeparam name="T">The message type that identifies which listeners will receive the message.</typeparam>
        void Publish<T>(T message);

        /// <summary>
        /// Subscribes to messages of a specified type.
        /// </summary>
        /// <param name="handler">The handler action that gets called when a message of type <typeparamref name="T"/> is published.</param>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>A disposable object that unsubscribes the handler when disposed.</returns>
        IDisposable Subscribe<T>(Action<T> handler);
    }
}