using System.Collections.Generic;
using System.Threading.Tasks;
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
        Task<ModuleConnection?> DeleteAsync(long id);

        /// <summary>
        /// Gets all connections.
        /// </summary>
        IAsyncEnumerable<ModuleConnection> GetAllAsync();

        /// <summary>
        /// Gets a connection with the specified ID or null if no connection exists with this ID.
        /// </summary>
        /// <param name="id">The ID of the connection.</param>
        Task<ModuleConnection?> GetByIdAsync(long id);

        /// <summary>
        /// Inserts a new connection to the DB.
        /// </summary>
        /// <param name="mc">The connection.</param>
        Task InsertAsync(ModuleConnection mc);

        /// <summary>
        /// Deletes all connections from the DB.
        /// </summary>
        Task TruncateAsync();

        /// <summary>
        /// Updates an existing connection.
        /// </summary>
        /// <param name="mc">The connection to update.</param>
        Task UpdateAsync(ModuleConnection mc);
    }
}