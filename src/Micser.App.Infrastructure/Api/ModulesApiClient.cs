using Micser.Common.Modules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    public class ModulesApiClient
    {
        public async Task<ServiceResult<ModuleDto>> CreateAsync(ModuleDto moduleDto)
        {
            return null;
        }

        public async Task<ServiceResult<ModuleDto>> DeleteAsync(long id)
        {
            return null;
        }

        public async Task<ServiceResult<IEnumerable<ModuleDto>>> GetAllAsync()
        {
            return null;
        }

        public async Task<ServiceResult<ModuleDto>> UpdateAsync(ModuleDto moduleDto)
        {
            return null;
        }
    }
}