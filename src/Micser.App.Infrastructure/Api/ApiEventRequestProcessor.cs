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

        public ApiEventRequestProcessor(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public JsonResponse Process(string action, object content)
        {
            var apiEvent = _eventAggregator.GetEvent<ApiEvent>();
            var data = new ApiEvent.ApiData(action, content);
            apiEvent.Publish(data);
            return data.Response ?? new JsonResponse(false);
        }
    }
}