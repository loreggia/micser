using System.Collections.Generic;
using System.Threading.Tasks;
using Micser.Common.Modules;

namespace Micser.Common.Services
{
    /// <summary>
    /// Provides access to saved engine modules.
    /// </summary>
    public interface IModuleService
    {
        /// <summary>
        /// Deletes a module.
        /// </summary>
        /// <param name="id">The ID of the module to delete.</param>
        /// <returns>The deleted module.</returns>
        Task<ModuleDto?> DeleteAsync(long id);

        /// <summary>
        /// Gets all modules.
        /// </summary>
        IAsyncEnumerable<ModuleDto> GetAllAsync();

        /// <summary>
        /// Gets a module with the specified ID or null if no module exists with this ID.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        Task<ModuleDto?> GetByIdAsync(long id);

        /// <summary>
        /// Inserts a new module to the DB.
        /// </summary>
        /// <param name="moduleDto">The module.</param>
        Task InsertAsync(ModuleDto moduleDto);

        /// <summary>
        /// Deletes all modules from the DB.
        /// </summary>
        Task TruncateAsync();

        /// <summary>
        /// Updates an existing module.
        /// </summary>
        /// <param name="moduleDto">The module to update.</param>
        Task UpdateAsync(ModuleDto moduleDto);
    }
}