using System;

namespace Micser.App.Infrastructure.Events
{
    public class EventSubscription<T> : IEventSubscription
    {
        public EventSubscription(Action<T> action)
        {
            Action = action;
        }

        public Action<T> Action { get; }

        public Action<object[]> GetExecutionStrategy()
        {
            var action = Action;

            if (action != null)
            {
                return arguments =>
                {
                    var argument = default(T);

                    if (arguments != null && arguments.Length > 0 && arguments[0] is T tArgument)
                    {
                        argument = tArgument;
                    }

                    action(argument);
                };
            }
            return null;
        }
    }

    public class EventSubscription : IEventSubscription
    {
        public EventSubscription(Action action)
        {
            Action = action;
        }

        public Action Action { get; }

        public Action<object[]> GetExecutionStrategy()
        {
            var action = Action;
            return _ => action();
        }
    }
}