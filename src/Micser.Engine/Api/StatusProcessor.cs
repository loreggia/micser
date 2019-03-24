using Micser.Common.Api;

namespace Micser.Engine.Api
{
    [RequestProcessorName("status")]
    public class StatusProcessor : IRequestProcessor
    {
        public JsonResponse Process(string action, object content)
        {
            return new JsonResponse(true, null, null);
        }
    }
}