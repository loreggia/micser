﻿using Micser.Common;
using Micser.Common.Api;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// API client that manages the engine state.
    /// </summary>
    public class EngineApiClient
    {
        private const string ResourceName = Globals.ApiResources.Engine;
        private readonly IApiEndPoint _apiEndPoint;

        /// <inheritdoc />
        public EngineApiClient(IApiEndPoint apiEndPoint)
        {
            _apiEndPoint = apiEndPoint;
        }

        /// <summary>
        /// Gets a value that indicates whether the engine is currently running.
        /// </summary>
        public async Task<ServiceResult<bool>> GetStatusAsync()
        {
            var response = await _apiEndPoint.SendMessageAsync(new JsonRequest(ResourceName, "getstatus")).ConfigureAwait(false);
            return new ServiceResult<bool>(response);
        }

        /// <summary>
        /// Restarts the engine.
        /// </summary>
        public async Task<ServiceResult<bool>> RestartAsync()
        {
            var response = await _apiEndPoint.SendMessageAsync(new JsonRequest(ResourceName, "restart")).ConfigureAwait(false);
            return new ServiceResult<bool>(response);
        }

        /// <summary>
        /// Starts the engine. Does nothing if the engine is already running.
        /// </summary>
        public async Task<ServiceResult<bool>> StartAsync()
        {
            var response = await _apiEndPoint.SendMessageAsync(new JsonRequest(ResourceName, "start")).ConfigureAwait(false);
            return new ServiceResult<bool>(response);
        }

        /// <summary>
        /// Stops the engine. Does nothing if the engine is already stopped.
        /// </summary>
        public async Task<ServiceResult<bool>> StopAsync()
        {
            var response = await _apiEndPoint.SendMessageAsync(new JsonRequest(ResourceName, "stop")).ConfigureAwait(false);
            return new ServiceResult<bool>(response);
        }
    }
}