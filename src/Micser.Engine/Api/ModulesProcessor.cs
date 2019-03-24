using Micser.Common.Api;

namespace Micser.Engine.Api
{
    [RequestProcessorName("modules")]
    public class ModulesProcessor : IRequestProcessor
    {
        public JsonResponse Process(string action, object content)
        {
            return new JsonResponse(true, null, null);
        }
    }
}