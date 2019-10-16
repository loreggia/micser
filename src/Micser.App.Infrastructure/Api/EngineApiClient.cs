using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Extensions;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// API client that manages the engine state.
    /// </summary>
    public class EngineApiClient
    {
        private const string ResourceName = Globals.ApiResources.Engine;
        private readonly IApiClient _apiClient;

        /// <inheritdoc />
        public EngineApiClient(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// Gets a value that indicates whether the engine is currently running.
        /// </summary>
        public async Task<ServiceResult<bool>> GetStatusAsync()
        {
            var response = await _apiClient.SendMessageAsync(ResourceName, "getstatus").ConfigureAwait(false);
            return new ServiceResult<bool>(response);
        }

        /// <summary>
        /// Restarts the engine.
        /// </summary>
        public async Task<ServiceResult<bool>> RestartAsync()
        {
            var response = await _apiClient.SendMessageAsync(ResourceName, "restart").ConfigureAwait(false);
            return new ServiceResult<bool>(response);
        }

        /// <summary>
        /// Starts the engine. Does nothing if the engine is already running.
        /// </summary>
        public async Task<ServiceResult<bool>> StartAsync()
        {
            var response = await _apiClient.SendMessageAsync(ResourceName, "start").ConfigureAwait(false);
            return new ServiceResult<bool>(response);
        }

        /// <summary>
        /// Stops the engine. Does nothing if the engine is already stopped.
        /// </summary>
        public async Task<ServiceResult<bool>> StopAsync()
        {
            var response = await _apiClient.SendMessageAsync(ResourceName, "stop").ConfigureAwait(false);
            return new ServiceResult<bool>(response);
        }
    }
}