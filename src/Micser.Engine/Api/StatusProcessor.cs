using Micser.Common;
using Micser.Common.Api;

namespace Micser.Engine.Api
{
    [RequestProcessorName(Globals.ApiResources.Status)]
    public class StatusProcessor : RequestProcessor
    {
        public StatusProcessor()
        {
            this[""] = _ => true;
        }
    }
}