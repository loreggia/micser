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

        public async Task<ServiceResult<ModuleDto>> CreateAsync(ModuleDto description)
        {
            return await PostAsync<ModuleDto>(null, description);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(long id)
        {
            return await DeleteAsync<bool>(null, id);
        }

        public async Task<ServiceResult<IEnumerable<ModuleDto>>> GetAllAsync()
        {
            return await GetAsync<IEnumerable<ModuleDto>>(null);
        }

        public async Task<ServiceResult<ModuleDto>> UpdateAsync(ModuleDto description)
        {
            return await PutAsync<ModuleDto>(null, description.Id, description);
        }
    }
}