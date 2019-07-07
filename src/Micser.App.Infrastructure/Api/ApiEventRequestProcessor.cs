using Micser.Common.Api;
using Prism.Events;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// Processes incoming requests from the engine and publishes an <see cref="ApiEvent"/>.
    /// </summary>
    public class ApiEventRequestProcessor : IRequestProcessor
    {
        private readonly IEventAggregator _eventAggregator;

        /// <inheritdoc />
        public ApiEventRequestProcessor(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        /// Publishes an <see cref="ApiEvent"/> via <see cref="IEventAggregator"/> for incoming requests.
        /// </summary>
        public Task<JsonResponse> ProcessAsync(string action, object content)
        {
            var apiEvent = _eventAggregator.GetEvent<ApiEvent>();
            var data = new ApiEvent.ApiData(action, content);
            apiEvent.Publish(data);
            var result = data.Response ?? new JsonResponse(false);
            return Task.FromResult(result);
        }
    }
}