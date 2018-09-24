using Nancy;

namespace Micser.Engine.Api.Modules
{
    public abstract class BaseModule : NancyModule
    {
        protected BaseModule(string modulePath)
            : base($"/api/{modulePath.Trim('/')}")
        {
        }
    }
}