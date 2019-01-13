using Nancy;

namespace Micser.Engine.Infrastructure
{
    public abstract class ApiController : NancyModule
    {
        protected ApiController(string modulePath)
            : base($"/api/{modulePath.Trim('/')}")
        {
        }
    }
}