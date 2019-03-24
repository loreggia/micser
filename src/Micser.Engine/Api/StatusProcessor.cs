using Micser.Common.Api;

namespace Micser.Engine.Api
{
    [RequestProcessorName("status")]
    public class StatusProcessor : RequestProcessor
    {
        public StatusProcessor()
        {
            this[""] = _ => true;
        }
    }
}