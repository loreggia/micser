using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Modules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// API client that manages module/widget connections.
    /// </summary>
    public class ModuleConnectionsApiClient
    {
        private const string ResourceName = Globals.ApiResources.ModuleConnections;
        private readonly IApiEndPoint _apiEndPoint;

        /// <inheritdoc />
        public ModuleConnectionsApiClient(IApiEndPoint apiEndPoint)
        {
            _apiEndPoint = apiEndPoint;
        }

        /// <summary>
        /// Creates a module connection and returns the updated DTO.
        /// </summary>
        public async Task<ServiceResult<ModuleConnectionDto>> CreateAsync(ModuleConnectionDto connectionDto)
        {
            var response = await _apiEndPoint.SendMessageAsync(new JsonRequest(ResourceName, "insert", connectionDto)).ConfigureAwait(false);
            return new ServiceResult<ModuleConnectionDto>(response);
        }

        /// <summary>
        /// Deletes a module connection and returns the deleted connection.
        /// </summary>
        /// <param name="id">The ID of the connection to delete.</param>
        public async Task<ServiceResult<ModuleConnectionDto>> DeleteAsync(long id)
        {
            var response = await _apiEndPoint.SendMessageAsync(new JsonRequest(ResourceName, "delete", id)).ConfigureAwait(false);
            return new ServiceResult<ModuleConnectionDto>(response);
        }

        /// <summary>
        /// Gets all connections.
        /// </summary>
        public async Task<ServiceResult<IEnumerable<ModuleConnectionDto>>> GetAllAsync()
        {
            var response = await _apiEndPoint.SendMessageAsync(new JsonRequest(ResourceName, "getall")).ConfigureAwait(false);
            return new ServiceResult<IEnumerable<ModuleConnectionDto>>(response);
        }

        /// <summary>
        /// Updates a connection and returns the validated/saved DTO.
        /// </summary>
        public async Task<ServiceResult<ModuleConnectionDto>> UpdateAsync(ModuleConnectionDto connectionDto)
        {
            var response = await _apiEndPoint.SendMessageAsync(new JsonRequest(ResourceName, "update", connectionDto)).ConfigureAwait(false);
            return new ServiceResult<ModuleConnectionDto>(response);
        }
    }
}