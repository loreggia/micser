using Micser.Common.Api;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    public class EngineApiClient
    {
        private const string ResourceName = "engine";
        private readonly IApiClient _apiClient;

        public EngineApiClient(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ServiceResult<bool>> RestartAsync()
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "restart"));
            return new ServiceResult<bool>(response);
        }

        public async Task<ServiceResult<bool>> StartAsync()
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "start"));
            return new ServiceResult<bool>(response);
        }

        public async Task<ServiceResult<bool>> StopAsync()
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "stop"));
            return new ServiceResult<bool>(response);
        }
    }
}