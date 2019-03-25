using Micser.Common.Api;
using Prism.Events;

namespace Micser.App.Infrastructure.Api
{
    public class ApiEvent : PubSubEvent<ApiEvent.ApiData>
    {
        public class ApiData
        {
            public ApiData(string action, object content)
            {
                Action = action;
                Content = content;
            }

            public string Action { get; }
            public object Content { get; }
            public JsonResponse Response { get; set; }
        }
    }
}