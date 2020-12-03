using System.Collections.Generic;
using Micser.Common.Modules;

namespace Micser.Common.Services
{
    /// <summary>
    /// Provides access to saved engine module connections.
    /// </summary>
    public interface IModuleConnectionService
    {
        /// <summary>
        /// Deletes a module connection.
        /// </summary>
        /// <param name="id">The ID of the connection to delete.</param>
        /// <returns>The deleted connection.</returns>
        ModuleConnectionDto Delete(long id);

        /// <summary>
        /// Gets all connections.
        /// </summary>
        IEnumerable<ModuleConnectionDto> GetAll();

        /// <summary>
        /// Gets a connection with the specified ID or null if no connection exists with this ID.
        /// </summary>
        /// <param name="id">The ID of the connection.</param>
        ModuleConnectionDto GetById(long id);

        /// <summary>
        /// Inserts a new connection to the DB.
        /// </summary>
        /// <param name="dto">The connection.</param>
        /// <returns>True if the connection was saved, otherwise false.</returns>
        bool Insert(ModuleConnectionDto dto);

        /// <summary>
        /// Deletes all connections from the DB.
        /// </summary>
        /// <returns>True if the connections were deleted, otherwise false.</returns>
        bool Truncate();

        /// <summary>
        /// Updates an existing connection.
        /// </summary>
        /// <param name="dto">The connection to update.</param>
        /// <returns>True if the connection was updated, otherwise false.</returns>
        bool Update(ModuleConnectionDto dto);
    }
}