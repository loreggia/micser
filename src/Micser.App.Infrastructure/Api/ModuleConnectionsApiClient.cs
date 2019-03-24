using Micser.Common.Api;
using Micser.Common.Modules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    public class ModuleConnectionsApiClient
    {
        private const string ResourceName = "moduleconnections";
        private readonly IApiClient _apiClient;

        public ModuleConnectionsApiClient(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ServiceResult<ModuleConnectionDto>> CreateAsync(ModuleConnectionDto connectionDto)
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "insert", connectionDto));
            return new ServiceResult<ModuleConnectionDto>(response);
        }

        public async Task<ServiceResult<ModuleConnectionDto>> DeleteAsync(long id)
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "delete", id));
            return new ServiceResult<ModuleConnectionDto>(response);
        }

        public async Task<ServiceResult<IEnumerable<ModuleConnectionDto>>> GetAllAsync()
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "getall"));
            return new ServiceResult<IEnumerable<ModuleConnectionDto>>(response);
        }

        public async Task<ServiceResult<ModuleConnectionDto>> UpdateAsync(ModuleConnectionDto connectionDto)
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "update", connectionDto));
            return new ServiceResult<ModuleConnectionDto>(response);
        }
    }
}