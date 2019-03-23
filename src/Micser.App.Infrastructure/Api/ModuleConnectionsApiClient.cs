using Micser.Common.Api;
using Micser.Common.Modules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    public class ModuleConnectionsApiClient : ApiClient
    {
        public async Task<ServiceResult<ModuleConnectionDto>> CreateAsync(ModuleConnectionDto connectionDto)
        {
            return null;
        }

        public async Task<ServiceResult<ModuleConnectionDto>> DeleteAsync(long id)
        {
            return null;
        }

        public async Task<ServiceResult<IEnumerable<ModuleConnectionDto>>> GetAllAsync()
        {
            return null;
        }

        public async Task<ServiceResult<ModuleConnectionDto>> UpdateAsync(ModuleConnectionDto connectionDto)
        {
            return null;
        }
    }
}