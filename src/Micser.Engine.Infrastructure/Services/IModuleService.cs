using Micser.Common.Modules;
using System.Collections.Generic;

namespace Micser.Engine.Infrastructure.Services
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
        ModuleDto Delete(long id);

        /// <summary>
        /// Gets all modules.
        /// </summary>
        IEnumerable<ModuleDto> GetAll();

        /// <summary>
        /// Gets a module with the specified ID or null if no module exists with this ID.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        ModuleDto GetById(long id);

        /// <summary>
        /// Inserts a new module to the DB.
        /// </summary>
        /// <param name="moduleDto">The module.</param>
        /// <returns>True if the module was saved, otherwise false.</returns>
        bool Insert(ModuleDto moduleDto);

        /// <summary>
        /// Deletes all modules from the DB.
        /// </summary>
        /// <returns>True if the modules were deleted, otherwise false.</returns>
        bool Truncate();

        /// <summary>
        /// Updates an existing module.
        /// </summary>
        /// <param name="moduleDto">The module to update.</param>
        /// <returns>True if the module was updated, otherwise false.</returns>
        bool Update(ModuleDto moduleDto);
    }
}