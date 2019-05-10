using System;
using System.Collections.Generic;

namespace Micser.Common.Api
{
    /// <summary>
    /// Base implementation of the <see cref="IRequestProcessor"/> interface.
    /// </summary>
    public abstract class RequestProcessor : IRequestProcessor
    {
        private readonly IDictionary<string, Func<dynamic, object>> _actions;

        /// <summary>
        /// Creates an instance of the <see cref="RequestProcessor"/> class.
        /// </summary>
        protected RequestProcessor()
        {
            _actions = new Dictionary<string, Func<dynamic, object>>();
        }

        /// <summary>
        /// Gets or sets a handler function that is executed when a request with the specified action name is received.
        /// </summary>
        /// <param name="action">The name of the action (case insensitive).</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> cannot be null.</exception>
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

        /// <summary>
        /// Processes a request.
        /// </summary>
        /// <param name="action">The name of the action function to execute.</param>
        /// <param name="param">The deserialized message content.</param>
        /// <returns>A <see cref="JsonResponse"/>; if no action with the specified action name is found, an empty response with <see cref="JsonResponse.IsSuccess"/>=false is returned.</returns>
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