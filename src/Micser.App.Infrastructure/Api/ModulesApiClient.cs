using Micser.Common.Modules;
using System;
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

        public async Task<ServiceResult<ModuleDescription>> CreateAsync(ModuleDescription description)
        {
            return await PostAsync<ModuleDescription>(null, description);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await DeleteAsync<bool>(null, id);
        }

        public async Task<ServiceResult<IEnumerable<ModuleDescription>>> GetAllAsync()
        {
            return await GetAsync<IEnumerable<ModuleDescription>>(null);
        }

        public async Task<ServiceResult<ModuleDescription>> UpdateAsync(ModuleDescription description)
        {
            return await PutAsync<ModuleDescription>(null, description.Id, description);
        }
    }
}