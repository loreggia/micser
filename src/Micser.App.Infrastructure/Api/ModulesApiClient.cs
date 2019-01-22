using System.Collections.Generic;
using System.Threading.Tasks;
using Micser.Common.Modules;

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

        public async Task<ServiceResult<ModuleDescription>> Create(ModuleDescription description)
        {
            return await PostAsync<ModuleDescription>(null, description);
        }

        public async Task<ServiceResult<ModuleDescription>> Update(ModuleDescription description)
        {
            return await PutAsync<ModuleDescription>(null, description.Id, description);
        }

        public async Task<ServiceResult<bool>> Delete(ModuleDescription description)
        {
            return await DeleteAsync<bool>(null, description.Id);
        }
    }
}