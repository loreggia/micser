﻿using Micser.Common.Api;

namespace Micser.Engine.Api
{
    [RequestProcessorName("moduleconnections")]
    public class ModuleConnectionsProcessor : IRequestProcessor
    {
        public JsonResponse Process(string action, object content)
        {
            return new JsonResponse(true, null, null);
        }
    }
}