using Micser.Common.Api;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// API client that manages the engine state.
    /// </summary>
    public class EngineApiClient
    {
        private const string ResourceName = "engine";
        private readonly IApiClient _apiClient;

        public EngineApiClient(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// Gets a value that indicates whether the engine is currently running.
        /// </summary>
        public async Task<ServiceResult<bool>> GetStatusAsync()
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "getstatus"));
            return new ServiceResult<bool>(response);
        }

        /// <summary>
        /// Restarts the engine.
        /// </summary>
        public async Task<ServiceResult<bool>> RestartAsync()
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "restart"));
            return new ServiceResult<bool>(response);
        }

        /// <summary>
        /// Starts the engine. Does nothing if the engine is already running.
        /// </summary>
        public async Task<ServiceResult<bool>> StartAsync()
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "start"));
            return new ServiceResult<bool>(response);
        }

        /// <summary>
        /// Stops the engine. Does nothing if the engine is already stopped.
        /// </summary>
        public async Task<ServiceResult<bool>> StopAsync()
        {
            var response = await _apiClient.SendMessageAsync(new JsonRequest(ResourceName, "stop"));
            return new ServiceResult<bool>(response);
        }
    }
}