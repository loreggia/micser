using Micser.Common.Api;
using Micser.Common.Modules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    public class ModulesApiClient
    {
        private const string ResourceName = "modules";
        private readonly IApiClient _apiClient;

        public ModulesApiClient(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ServiceResult<ModuleDto>> CreateAsync(ModuleDto moduleDto)
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "insert", moduleDto));
            return new ServiceResult<ModuleDto>(response);
        }

        public async Task<ServiceResult<ModuleDto>> DeleteAsync(long id)
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "delete", id));
            return new ServiceResult<ModuleDto>(response);
        }

        public async Task<ServiceResult<IEnumerable<ModuleDto>>> GetAllAsync()
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "getall"));
            return new ServiceResult<IEnumerable<ModuleDto>>(response);
        }

        public async Task<ServiceResult<ModuleDto>> UpdateAsync(ModuleDto moduleDto)
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "update", moduleDto));
            return new ServiceResult<ModuleDto>(response);
        }
    }
}