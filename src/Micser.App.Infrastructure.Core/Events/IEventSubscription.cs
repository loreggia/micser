using System;

namespace Micser.App.Infrastructure.Events
{
    public interface IEventSubscription
    {
        Action<object> Handler { get; }
    }
}