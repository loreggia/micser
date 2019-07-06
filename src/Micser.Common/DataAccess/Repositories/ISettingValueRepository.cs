using Micser.Common.DataAccess.Models;

namespace Micser.Common.DataAccess.Repositories
{
    /// <summary>
    /// Provides database access to the settings store.
    /// </summary>
    public interface ISettingValueRepository : IRepository<SettingValue>
    {
        /// <summary>
        /// Finds a <see cref="SettingValue"/> object with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The setting key.</param>
        /// <returns>The setting value or null if no setting with this key was found.</returns>
        SettingValue GetByKey(string key);
    }
}