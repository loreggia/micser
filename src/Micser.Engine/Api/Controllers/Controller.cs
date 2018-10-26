using Nancy;

namespace Micser.Engine.Api.Controllers
{
    public abstract class Controller : NancyModule
    {
        protected Controller(string modulePath)
            : base($"/api/{modulePath.Trim('/')}")
        {
        }
    }
}