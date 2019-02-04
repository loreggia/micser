using Micser.Common.Modules;

namespace Micser.App.Infrastructure.Api
{
    public class ModulesApiClient : CrudApiClient<ModuleDto>
    {
        public ModulesApiClient()
            : base("modules")
        {
        }
    }
}