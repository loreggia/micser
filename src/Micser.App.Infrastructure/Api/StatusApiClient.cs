using Micser.Common.Api;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    public class StatusApiClient
    {
        private const string ResourceName = "status";
        private readonly IApiClient _apiClient;

        public StatusApiClient(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ServiceResult<object>> GetStatus()
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName));
            return new ServiceResult<object>(response);
        }
    }
}