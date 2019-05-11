using Micser.Common.Api;
using Prism.Events;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// Processes incoming requests from the engine and publishes an <see cref="ApiEvent"/>.
    /// </summary>
    public class ApiEventRequestProcessor : IRequestProcessor
    {
        private readonly IEventAggregator _eventAggregator;

        /// <summary>
        /// Creates an instance of the <see cref="ApiEventRequestProcessor"/> class.
        /// </summary>
        public ApiEventRequestProcessor(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        /// Publishes an <see cref="ApiEvent"/> via <see cref="IEventAggregator"/> for incoming requests.
        /// </summary>
        public JsonResponse Process(string action, object content)
        {
            var apiEvent = _eventAggregator.GetEvent<ApiEvent>();
            var data = new ApiEvent.ApiData(action, content);
            apiEvent.Publish(data);
            return data.Response ?? new JsonResponse(false);
        }
    }
}