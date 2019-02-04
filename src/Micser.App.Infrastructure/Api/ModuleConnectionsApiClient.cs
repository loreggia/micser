using Micser.Common.Modules;

namespace Micser.App.Infrastructure.Api
{
    public class ModuleConnectionsApiClient : CrudApiClient<ModuleConnectionDto>
    {
        public ModuleConnectionsApiClient()
            : base("moduleconnections")
        {
        }
    }
}