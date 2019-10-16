using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Extensions;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// API service client that allows checking if the service API is available.
    /// </summary>
    public class StatusApiClient
    {
        private const string ResourceName = Globals.ApiResources.Status;
        private readonly IApiClient _apiClient;

        /// <inheritdoc />
        public StatusApiClient(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// Returns true if the API is available.
        /// </summary>
        public async Task<ServiceResult<object>> GetStatus()
        {
            var response = await _apiClient.SendMessageAsync(ResourceName).ConfigureAwait(false);
            return new ServiceResult<object>(response);
        }
    }
}