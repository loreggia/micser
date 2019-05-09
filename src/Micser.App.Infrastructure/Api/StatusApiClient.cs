using Micser.Common.Api;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// API service client that allows checking if the service API is available.
    /// </summary>
    public class StatusApiClient
    {
        private const string ResourceName = "status";
        private readonly IApiClient _apiClient;

        public StatusApiClient(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// Returns true if the API is available.
        /// </summary>
        public async Task<ServiceResult<object>> GetStatus()
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName));
            return new ServiceResult<object>(response);
        }
    }
}