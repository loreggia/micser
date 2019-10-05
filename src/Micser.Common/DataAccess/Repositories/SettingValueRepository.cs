using Microsoft.EntityFrameworkCore;
using Micser.Common.DataAccess.Models;
using System.Linq;

namespace Micser.Common.DataAccess.Repositories
{
    /// <inheritdoc cref="ISettingValueRepository" />
    public class SettingValueRepository : Repository<SettingValue>, ISettingValueRepository
    {
        /// <inheritdoc />
        public SettingValueRepository(DbContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public SettingValue GetByKey(string key)
        {
            return DbSet.FirstOrDefault(s => s.Key == key);
        }
    }
}