using Micser.Infrastructure.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.Infrastructure.Api
{
    public class ModulesApiClient : ApiClient
    {
        public ModulesApiClient()
            : base("modules")
        {
        }

        public async Task<ServiceResult<IEnumerable<Module>>> GetAll()
        {
            return await GetAsync<IEnumerable<Module>>(null);
        }
    }
}