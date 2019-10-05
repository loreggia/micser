using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    /// <summary>
    /// Base implementation of the <see cref="IRequestProcessor"/> interface.
    /// </summary>
    public abstract class RequestProcessor : IRequestProcessor
    {
        private readonly IDictionary<string, Func<dynamic, Task<object>>> _actions;

        /// <inheritdoc />
        protected RequestProcessor()
        {
            _actions = new Dictionary<string, Func<dynamic, Task<object>>>();
        }

        /// <summary>
        /// Gets or sets a handler function that is executed when a request with the specified action name is received.
        /// </summary>
        /// <param name="name">The name of the action (case insensitive).</param>
        /// <param name="action">An action function.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> cannot be null.</exception>
        public void AddAction(string name, Func<dynamic, object> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            name = name ?? "";

            _actions[name.ToLower()] = async x => await Task.FromResult(action(x));
        }

        /// <summary>
        /// Gets or sets a handler function that is executed when a request with the specified action name is received.
        /// </summary>
        /// <param name="name">The name of the action (case insensitive).</param>
        /// <param name="action">An async action function.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> cannot be null.</exception>
        public void AddAsyncAction(string name, Func<dynamic, Task<object>> action)
        {
            name = name ?? "";
            _actions[name.ToLower()] = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>
        /// Processes a request.
        /// </summary>
        /// <param name="action">The name of the action function to execute.</param>
        /// <param name="param">The deserialized message content.</param>
        /// <returns>A <see cref="JsonResponse"/>; if no action with the specified action name is found, an empty response with <see cref="JsonResponse.IsSuccess"/>=false is returned.</returns>
        public virtual async Task<JsonResponse> ProcessAsync(string action, object param)
        {
            if (_actions.TryGetValue(action ?? "", out var actionFunction) ||
                _actions.TryGetValue("", out actionFunction))
            {
                try
                {
                    var result = await actionFunction.Invoke(param).ConfigureAwait(false);

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