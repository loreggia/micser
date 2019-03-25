using System;
using System.Collections.Generic;

namespace Micser.Common.Api
{
    public abstract class RequestProcessor : IRequestProcessor
    {
        private readonly IDictionary<string, Func<dynamic, object>> _actions;

        protected RequestProcessor()
        {
            _actions = new Dictionary<string, Func<dynamic, object>>();
        }

        public Func<dynamic, object> this[string action]
        {
            get
            {
                if (action == null)
                {
                    throw new ArgumentNullException(nameof(action));
                }

                if (_actions.TryGetValue(action.ToLower(), out var function))
                {
                    return function;
                }

                return null;
            }
            set
            {
                if (action == null)
                {
                    throw new ArgumentNullException(nameof(action));
                }

                _actions[action.ToLower()] = value;
            }
        }

        public virtual JsonResponse Process(string action, object param)
        {
            if (_actions.TryGetValue(action ?? "", out var actionFunction) ||
                _actions.TryGetValue("", out actionFunction))
            {
                try
                {
                    var result = actionFunction(param);

                    if (result is bool b)
                    {
                        return new JsonResponse(b);
                    }

                    if (result is JsonResponse response)
                    {
                        return response;
                    }

                    return new JsonResponse(true, result);
                }
                catch (Exception ex)
                {
                    return new JsonResponse(false, ex.ToString(), ex.Message);
                }
            }

            return new JsonResponse(false, null, $"No handler for action '{action}' found on '{GetType().Name}'");
        }
    }
}