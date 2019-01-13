using Micser.Common.Modules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    public class ModulesApiClient : ApiClient
    {
        public ModulesApiClient()
            : base("modules")
        {
        }

        public async Task<ServiceResult<IEnumerable<ModuleDescription>>> GetAll()
        {
            return await GetAsync<IEnumerable<ModuleDescription>>(null);
        }
    }
}