using System;

namespace Micser.Common.Events
{
    public interface IEventAggregator
    {
        void Publish<T>(T message);

        IDisposable Subscribe<T>(Action<T> handler);
    }
}