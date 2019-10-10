using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Modules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// API client that manages modules/widgets.
    /// </summary>
    public class ModulesApiClient
    {
        private const string ResourceName = Globals.ApiResources.Modules;
        private readonly IApiClient _apiClient;

        /// <inheritdoc />
        public ModulesApiClient(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// Creates a new module and returns the saved DTO.
        /// </summary>
        public async Task<ServiceResult<ModuleDto>> CreateAsync(ModuleDto moduleDto)
        {
            var response = await _apiClient.SendMessageAsync(new ApiRequest(ResourceName, "insert", moduleDto)).ConfigureAwait(false);
            return new ServiceResult<ModuleDto>(response);
        }

        /// <summary>
        /// Deletes a module and returns the deleted DTO.
        /// </summary>
        public async Task<ServiceResult<ModuleDto>> DeleteAsync(long id)
        {
            var response = await _apiClient.SendMessageAsync(new ApiRequest(ResourceName, "delete", id)).ConfigureAwait(false);
            return new ServiceResult<ModuleDto>(response);
        }

        /// <summary>
        /// Gets all modules.
        /// </summary>
        public async Task<ServiceResult<IEnumerable<ModuleDto>>> GetAllAsync()
        {
            var response = await _apiClient.SendMessageAsync(new ApiRequest(ResourceName, "getall")).ConfigureAwait(false);
            return new ServiceResult<IEnumerable<ModuleDto>>(response);
        }

        /// <summary>
        /// Stops the engine if it is running, deletes all existing modules and connections,
        /// imports all modules and connections from the specified data and then restarts the engine if it was running before the import.
        /// </summary>
        public async Task<bool> ImportConfigurationAsync(ModulesExportDto dto)
        {
            var response = await _apiClient.SendMessageAsync(new ApiRequest(ResourceName, "import", dto)).ConfigureAwait(false);
            return response.IsSuccess;
        }

        /// <summary>
        /// Updates a module and returns the saved DTO.
        /// </summary>
        public async Task<ServiceResult<ModuleDto>> UpdateAsync(ModuleDto moduleDto)
        {
            var response = await _apiClient.SendMessageAsync(new ApiRequest(ResourceName, "update", moduleDto)).ConfigureAwait(false);
            return new ServiceResult<ModuleDto>(response);
        }
    }
}